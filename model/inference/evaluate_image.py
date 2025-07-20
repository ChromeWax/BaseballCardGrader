from pathlib import Path
import json
from PIL import Image
from functools import partial
import pandas as pd
import sys

import torch
import torchvision.transforms.v2 as transforms
import torch.nn.functional as F
from torchvision.tv_tensors import BoundingBoxes, Mask
from torchvision.utils import draw_bounding_boxes, draw_segmentation_masks
from torchvision.models.detection import maskrcnn_resnet50_fpn_v2
from torchvision.models.detection.faster_rcnn import FastRCNNPredictor
from torchvision.models.detection.mask_rcnn import MaskRCNNPredictor

def get_torch_device():
    if torch.cuda.is_available():
        return "cuda"
    else:
        return "cpu"

def tensor_to_pil(tensor):
    transform = transforms.ToPILImage()
    img = transform(tensor)
    return img

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

if __name__=="__main__":
    if len(sys.argv) != 2:
        print("Needs path to image")
        exit()
    '''
    Path for checkpoint, colormap, and other files
    '''
    checkpoint_directory = Path("./Checkpoints")
    checkpoint_path = list(checkpoint_directory.glob('*.pth'))[0]
    colormap_path = list(checkpoint_directory.glob('*colormap.json'))[0]
    test_image_path = Path(sys.argv[1])
    font_file = 'KFOlCnqEu92Fr1MmEU9vAw.ttf'

    '''
    Gets device and dtype for evaluating
    '''
    device = get_torch_device()
    dtype = torch.float32

    '''
    Gets colormap, classnames, and colors from json file
    '''
    with open(colormap_path, 'r') as file:
        colormap_json = json.load(file)
    colormap_dict = {item['label']: item['color'] for item in colormap_json['items']}
    class_names = list(colormap_dict.keys())
    int_colors = [tuple(int(c*255) for c in color) for color in colormap_dict.values()]

    '''
    Sets up model from checkpoint
    '''
    model_checkpoint = torch.load(checkpoint_path, map_location='cpu', weights_only=True)
    model = maskrcnn_resnet50_fpn_v2(weights='DEFAULT')
    in_features_box = model.roi_heads.box_predictor.cls_score.in_features
    in_features_mask = model.roi_heads.mask_predictor.conv5_mask.in_channels
    model.roi_heads.box_predictor = FastRCNNPredictor(in_features_box, len(class_names))
    model.roi_heads.mask_predictor = MaskRCNNPredictor(in_channels=in_features_mask, dim_reduced=256, num_classes=len(class_names))
    model.load_state_dict(model_checkpoint)

    '''
    Evaluates image with model
    '''
    model.eval()
    model.to(device)

    test_image = Image.open(test_image_path).convert("RGB")
    resized_image = transforms.Resize([640, 480], antialias=True)(test_image)
    input_tensor = transforms.Compose([transforms.ToImage(), transforms.ToDtype(torch.float32, scale=True)])(resized_image)[None].to(device)

    with torch.no_grad():
        model_output = model(input_tensor)

    '''
    Filters out information from evaluation
    '''
    threshold = 0.5
    model_output = move_data_to_device(model_output, 'cpu')
    scores_mask = model_output[0]['scores'] > threshold
    pred_bboxes = BoundingBoxes(model_output[0]['boxes'][scores_mask], format='xyxy', canvas_size=resized_image.size[::-1])
    pred_labels = [class_names[int(label)] for label in model_output[0]['labels'][scores_mask]]
    pred_scores = model_output[0]['scores']
    pred_masks = F.interpolate(model_output[0]['masks'][scores_mask], size=resized_image.size[::-1])
    pred_masks = torch.concat([Mask(torch.where(mask >= threshold, 1, 0), dtype=torch.bool) for mask in pred_masks])

    '''
    Displays evaluation
    '''
    draw_bboxes = partial(draw_bounding_boxes, fill=False, width=2, font=font_file, font_size=25)
    pred_colors=[int_colors[i] for i in [class_names.index(label) for label in pred_labels]]

    image_tensor = transforms.PILToTensor()(resized_image)
    annotated_tensor = draw_segmentation_masks(image=image_tensor, masks=pred_masks, alpha=0.3, colors=pred_colors)
    annotated_tensor = draw_bboxes(
        image=annotated_tensor, 
        boxes=pred_bboxes, 
        labels=[f"{label}\n{prob*100:.2f}%" for label, prob in zip(pred_labels, pred_scores)],
        colors=pred_colors
    )

    output_image = tensor_to_pil(annotated_tensor).show()
    print(pd.Series({
        "Predicted BBoxes:": [f"{label}:{bbox}" for label, bbox in zip(pred_labels, pred_bboxes.round(decimals=3).numpy())],
        "Confidence Scores:": [f"{label}: {prob*100:.2f}%" for label, prob in zip(pred_labels, pred_scores)]
    }).to_frame().style.hide(axis='columns').to_string())
