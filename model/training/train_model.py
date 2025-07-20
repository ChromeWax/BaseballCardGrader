from pathlib import Path
from functools import partial
import pandas as pd
import random
import math
import json
from tqdm.auto import tqdm

# Used to create unique colors for each class
from distinctipy import distinctipy

# Import PIL for image manipulation
from PIL import Image, ImageDraw

# Import PyTorch dependencies
import torch
from torch.amp import autocast
from torch.utils.data import Dataset, DataLoader
import torchvision
torchvision.disable_beta_transforms_warning()
from torchvision.tv_tensors import BoundingBoxes, Mask
from torchvision.utils import draw_bounding_boxes
import torchvision.transforms.v2 as transforms

# Import Mask R-CNN
from torchvision.models.detection import maskrcnn_resnet50_fpn_v2
from torchvision.models.detection.faster_rcnn import FastRCNNPredictor
from torchvision.models.detection.mask_rcnn import MaskRCNNPredictor

class BaseballCardDataset(Dataset):
    def __init__(self, img_keys, annotation_df, img_dict, class_to_idx, transforms=None):
        super(Dataset, self).__init__()
        
        self._img_keys = img_keys  # List of image keys
        self._annotation_df = annotation_df  # DataFrame containing annotations
        self._img_dict = img_dict  # Dictionary mapping image keys to image paths
        self._class_to_idx = class_to_idx  # Dictionary mapping class names to class indices
        self._transforms = transforms  # Image transforms to be applied
        
    def __len__(self):
        return len(self._img_keys)
        
    def __getitem__(self, index):
        # Retrieve the key for the image at the specified index
        img_key = self._img_keys[index]
        # Get the annotations for this image
        annotation = self._annotation_df.loc[img_key]
        # Load the image and its target (segmentation masks, bounding boxes and labels)
        image, target = self._load_image_and_target(annotation)
        
        # Apply the transformations, if any
        if self._transforms:
            image, target = self._transforms(image, target)
        
        return image, target

    def _load_image_and_target(self, annotation):
        # Retrieve the file path of the image
        filepath = self._img_dict[annotation.name]
        # Open the image file and convert it to RGB
        image = Image.open(filepath).convert('RGB')
        
        # Convert the class labels to indices
        labels = [shape['label'] for shape in annotation['shapes']]
        labels = torch.Tensor([self._class_to_idx[label] for label in labels])
        labels = labels.to(dtype=torch.int64)

        # Convert polygons to mask images
        shape_points = [shape['points'] for shape in annotation['shapes']]
        xy_coords = [[tuple(p) for p in points] for points in shape_points]
        mask_imgs = [create_polygon_mask(image.size, xy) for xy in xy_coords]
        masks = Mask(torch.concat([Mask(transforms.PILToTensor()(mask_img), dtype=torch.bool) for mask_img in mask_imgs]))

        # Generate bounding box annotations from segmentation masks
        bboxes = BoundingBoxes(data=torchvision.ops.masks_to_boxes(masks), format='xyxy', canvas_size=image.size[::-1])
                
        return image, {'masks': masks,'boxes': bboxes, 'labels': labels}

def create_polygon_mask(image_size, vertices):
    mask_image = Image.new('L', image_size, 0)
    ImageDraw.Draw(mask_image, 'L').polygon(vertices, fill=(255))
    return mask_image

def get_torch_device():
    if torch.cuda.is_available():
        return "cuda"
    else:
        return "cpu"

def tensor_to_pil(tensor):
    transform = transforms.ToPILImage()
    img = transform(tensor)
    return img

def custom_collate_fn(batch):
    return tuple(zip(*batch))

def move_data_to_device(data, device:torch.device):
    if isinstance(data, tuple):
        return tuple(move_data_to_device(d, device) for d in data)
    
    if isinstance(data, list):
        return list(move_data_to_device(d, device) for d in data)
    
    elif isinstance(data, dict):
        return {k: move_data_to_device(v, device) for k, v in data.items()}
    
    elif isinstance(data, torch.Tensor):
        return data.to(device)
    
    else:
        return data

def run_epoch(model, dataloader, optimizer, lr_scheduler, device, scaler, epoch_id, is_training):
    """
    Function to run a single training or evaluation epoch.
    
    Args:
        model: A PyTorch model to train or evaluate.
        dataloader: A PyTorch DataLoader providing the data.
        optimizer: The optimizer to use for training the model.
        loss_func: The loss function used for training.
        device: The device (CPU or GPU) to run the model on.
        scaler: Gradient scaler for mixed-precision training.
        is_training: Boolean flag indicating whether the model is in training or evaluation mode.
    
    Returns:
        The average loss for the epoch.
    """
    # Set the model to training mode
    model.train()
    
    epoch_loss = 0  # Initialize the total loss for this epoch
    progress_bar = tqdm(total=len(dataloader), desc="Train" if is_training else "Eval")  # Initialize a progress bar
    
    # Loop over the data
    for batch_id, (inputs, targets) in enumerate(dataloader):
        # Move inputs and targets to the specified device
        inputs = torch.stack(inputs).to(device)
        
        # Forward pass with Automatic Mixed Precision (AMP) context manager
        with autocast(torch.device(device).type):
            if is_training:
                losses = model(inputs.to(device), move_data_to_device(targets, device))
            else:
                with torch.no_grad():
                    losses = model(inputs.to(device), move_data_to_device(targets, device))
        
            # Compute the loss
            loss = sum([loss for loss in losses.values()])  # Sum up the losses

        # If in training mode, backpropagate the error and update the weights
        if is_training:
            if scaler:
                scaler.scale(loss).backward()
                scaler.step(optimizer)
                old_scaler = scaler.get_scale()
                scaler.update()
                new_scaler = scaler.get_scale()
                if new_scaler >= old_scaler:
                    lr_scheduler.step()
            else:
                loss.backward()
                optimizer.step()
                lr_scheduler.step()
                
            optimizer.zero_grad()

        # Update the total loss
        loss_item = loss.item()
        epoch_loss += loss_item
        
        # Update the progress bar
        progress_bar_dict = dict(loss=loss_item, avg_loss=epoch_loss/(batch_id+1))
        if is_training:
            progress_bar_dict.update(lr=lr_scheduler.get_last_lr()[0])
        progress_bar.set_postfix(progress_bar_dict)
        progress_bar.update()

        # If loss is NaN or infinity, stop training
        if is_training:
            stop_training_message = f"Loss is NaN or infinite at epoch {epoch_id}, batch {batch_id}. Stopping training."
            assert not math.isnan(loss_item) and math.isfinite(loss_item), stop_training_message

    # Cleanup and close the progress bar 
    progress_bar.close()
    
    # Return the average loss for this epoch
    return epoch_loss / (batch_id + 1)

def train_loop(model, 
               train_dataloader, 
               valid_dataloader, 
               optimizer,  
               lr_scheduler, 
               device, 
               epochs, 
               checkpoint_path, 
               use_scaler=False):
    """
    Main training loop.
    
    Args:
        model: A PyTorch model to train.
        train_dataloader: A PyTorch DataLoader providing the training data.
        valid_dataloader: A PyTorch DataLoader providing the validation data.
        optimizer: The optimizer to use for training the model.
        lr_scheduler: The learning rate scheduler.
        device: The device (CPU or GPU) to run the model on.
        epochs: The number of epochs to train for.
        checkpoint_path: The path where to save the best model checkpoint.
        use_scaler: Whether to scale graidents when using a CUDA device
    
    Returns:
        None
    """
    # Initialize a gradient scaler for mixed-precision training if the device is a CUDA GPU
    scaler = torch.amp.GradScaler() if device.type == 'cuda' and use_scaler else None
    best_loss = float('inf')  # Initialize the best validation loss

    # Loop over the epochs
    for epoch in tqdm(range(epochs), desc="Epochs"):
        # Run a training epoch and get the training loss
        train_loss = run_epoch(model, train_dataloader, optimizer, lr_scheduler, device, scaler, epoch, is_training=True)
        # Run an evaluation epoch and get the validation loss
        with torch.no_grad():
            valid_loss = run_epoch(model, valid_dataloader, None, None, device, scaler, epoch, is_training=False)

        # If the validation loss is lower than the best validation loss seen so far, save the model checkpoint
        if valid_loss < best_loss:
            best_loss = valid_loss
            torch.save(model.state_dict(), checkpoint_path)

            # Save metadata about the training process
            training_metadata = {
                'epoch': epoch,
                'train_loss': train_loss,
                'valid_loss': valid_loss, 
                'learning_rate': lr_scheduler.get_last_lr()[0],
                'model_architecture': model.name
            }
            with open(Path(checkpoint_path.parent/'training_metadata.json'), 'w') as f:
                json.dump(training_metadata, f)

    # If the device is a GPU, empty the cache
    if device.type != 'cpu':
        getattr(torch, device.type).empty_cache()

if __name__ ==  "__main__":
    '''
    These are specific paths for dataset, checkpoints, and other files
    '''
    # Path for dataset
    dataset_path = Path("./Images")
    image_file_paths = list(dataset_path.glob("*.jpg"))
    annotation_file_paths = list(dataset_path.glob("*.json"))

    # Name of the font file
    font_file = 'KFOlCnqEu92Fr1MmEU9vAw.ttf'

    '''
    This sets up the device and datatype
    '''
    # Gets device for training
    device = get_torch_device()
    dtype = torch.float32

    '''
    This grabs the dataset and creates a dataframe for them
    '''
    # Dictionary to map each file name to path
    image_dict = {file.stem : file for file in image_file_paths}

    # Dataframe for annotations
    cls_dataframes = (pd.read_json(file_path, orient='index').transpose() for file_path in annotation_file_paths)
    annotation_df = pd.concat(cls_dataframes, ignore_index=False)
    annotation_df['index'] = annotation_df.apply(lambda row: row['imagePath'].split('.')[0], axis=1)
    annotation_df = annotation_df.set_index('index')

    # Dataframe for segmentations of annotations
    shapes_df = annotation_df['shapes'].explode().to_frame().shapes.apply(pd.Series)

    # Gets unique list of classes, in this case just one
    class_names = shapes_df['label'].unique().tolist()

    # The Mask R-CNN model expects datasets to have a background class
    class_names = ['background'] + class_names

    # Generate a list of colors with a length equal to the number of labels
    colors = distinctipy.get_colors(len(class_names))
    int_colors = [tuple(int(c*255) for c in color) for color in colors]

    draw_bboxes = partial(draw_bounding_boxes, fill=False, width=2, font=font_file, font_size=25)

    '''
    This loads the Mask R-CNN Model 
    '''
    # Initialize a Mask R-CNN model with pretrained weights
    model = maskrcnn_resnet50_fpn_v2(weights='DEFAULT')

    # Get the number of input features for the classifier
    in_features_box = model.roi_heads.box_predictor.cls_score.in_features
    in_features_mask = model.roi_heads.mask_predictor.conv5_mask.in_channels

    # Get the numbner of output channels for the Mask Predictor
    dim_reduced = model.roi_heads.mask_predictor.conv5_mask.out_channels

    # Replace the box predictor
    model.roi_heads.box_predictor = FastRCNNPredictor(in_channels=in_features_box, num_classes=len(class_names))

    # Replace the mask predictor
    model.roi_heads.mask_predictor = MaskRCNNPredictor(in_channels=in_features_mask, dim_reduced=dim_reduced, num_classes=len(class_names))

    # Set the model's device and data type
    model.to(device=device, dtype=dtype);

    # Add attributes to store the device and model name for later reference
    model.device = device
    model.name = 'maskrcnn_resnet50_fpn_v2'

    '''
    This splits the available data into training and validation data
    '''
    # Get list of image IDs
    image_keys = list(image_dict.keys())

    # Shuffles IDs
    random.shuffle(image_keys)

    # Split the subset of image paths into training and validation sets
    train_percentage = 0.8
    train_keys = image_keys[ : int(len(image_keys) * train_percentage)]
    valid_keys = image_keys[int(len(image_keys) * train_percentage) : ]

    '''
    This creates additional training and validation data
    '''
    data_augment_transforms = transforms.Compose(
        transforms=[
            transforms.ColorJitter(
                    brightness = (0.875, 1.125),
                    contrast = (0.5, 1.5),
                    saturation = (0.5, 1.5),
                    hue = (-0.05, 0.05),
            ),
            transforms.RandomGrayscale(),
            transforms.RandomEqualize(),
            transforms.RandomPosterize(bits=3, p=0.5),
            transforms.RandomHorizontalFlip(p=0.5),
            transforms.RandomVerticalFlip(p=0.5),
        ],
    )

    # Compose transforms to sanitize bounding boxes and normalize input data
    final_tranforms = transforms.Compose([
        transforms.ToImage(), 
        transforms.ToDtype(torch.float32, scale=True),
        transforms.SanitizeBoundingBoxes(),
    ])

    # Compose transforms to resize and pad input images
    resize_tranforms = transforms.Compose([
        transforms.Resize([640, 480], antialias=True)
    ])

    # Define the transformations for training and validation datasets
    train_tfms = transforms.Compose([
        data_augment_transforms, 
        resize_tranforms,
        final_tranforms
    ])
    valid_tfms = transforms.Compose([
        resize_tranforms,
        final_tranforms
    ])

    '''
    Initialize Datasets
    '''
    # Create a mapping from class names to class indices
    class_to_idx = {c: i for i, c in enumerate(class_names)}

    # Instantiate the datasets using the defined transformations
    train_dataset = BaseballCardDataset(train_keys, annotation_df, image_dict, class_to_idx, train_tfms)
    valid_dataset = BaseballCardDataset(valid_keys, annotation_df, image_dict, class_to_idx, valid_tfms)

    '''
    Initialize Dataloaders
    '''
    # Set the training batch size
    bs = 4

    # Define parameters for DataLoader
    data_loader_params = {
        'batch_size': bs,  # Batch size for data loading
        'num_workers': 4,  # Number of subprocesses to use for data loading
        'persistent_workers': True,  # If True, the data loader will not shutdown the worker processes after a dataset has been consumed once. This allows to maintain the worker dataset instances alive.
        'pin_memory': 'cuda' in device,  # If True, the data loader will copy Tensors into CUDA pinned memory before returning them. Useful when using GPU.
        'pin_memory_device': device if 'cuda' in device else '',  # Specifies the device where the data should be loaded. Commonly set to use the GPU.
        'collate_fn': custom_collate_fn,
    }

    # Create DataLoader for training data. Data is shuffled for every epoch.
    train_dataloader = DataLoader(train_dataset, **data_loader_params, shuffle=True)

    # Create DataLoader for validation data. Shuffling is not necessary for validation data.
    valid_dataloader = DataLoader(valid_dataset, **data_loader_params)

    '''
    Set Model Checkpoint
    '''
    # Create a directory to store the checkpoints if it does not already exist
    checkpoint_directory = Path("./Checkpoints")
    # The model checkpoint path
    checkpoint_path = checkpoint_directory/f"{model.name}.pth"

    '''
    Saves Color Map
    '''
    # Create a color map and write it to a JSON file
    color_map = {'items': [{'label': label, 'color': color} for label, color in zip(class_names, colors)]}
    with open(f"{checkpoint_directory}/{dataset_path.name}-colormap.json", "w") as file:
        json.dump(color_map, file)

    '''
    Configures training parameters
    '''
    # Learning rate for the model
    lr = 5e-4

    # Number of training epochs
    epochs = 40

    # AdamW optimizer; includes weight decay for regularization
    optimizer = torch.optim.AdamW(model.parameters(), lr=lr)

    # Learning rate scheduler; adjusts the learning rate during training
    lr_scheduler = torch.optim.lr_scheduler.OneCycleLR(optimizer, max_lr=lr, total_steps=epochs*len(train_dataloader))

    '''
    Trains Model
    '''
    train_loop(model=model, 
            train_dataloader=train_dataloader,
            valid_dataloader=valid_dataloader,
            optimizer=optimizer, 
            lr_scheduler=lr_scheduler, 
            device=torch.device(device), 
            epochs=epochs, 
            checkpoint_path=checkpoint_path,
            use_scaler=True)
