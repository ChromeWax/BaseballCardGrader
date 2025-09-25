# Baseball Card Grader

The BaseBall Card Grader project goal is to find and detect defects in a card's surface. The project is a combination of a physical device and a mobile app. The physical device is a box that contains 4 lights and an ESP32. The user can place a card into the box, connect the box to the app via bluetooth, and then on the app evaluate the image.

The general process is to take 4 images, each taken with a different lighting position, composite those 4 images into a normal map, which represents 3D detail, and then run our model on the normal map for the annotated image.

https://github.com/user-attachments/assets/8a07d7a1-3c0c-4498-98e6-cceb2a37a0a0

## Mobile App
This app was built with MAUI Blazor Hybrid with .NET 9. Visit the path `BaseballCardGrader/Apps` for the relevant code to this section. 

### Image Processor Project
This project is responsible for compositing the 4 images into a normal map and inferencing the ONNX model for annotations. The SkiaSharp library was the only one found to be performant on mobile.

#### Helpful Guides
The following guides are useful for the process of creating normal map and running ONNX inferfence.
- [ONNX Inference](https://onnxruntime.ai/docs/tutorials/csharp/fasterrcnn_csharp.html)
- [Normal Map Process](http://zarria.net/nrmphoto/nrmphoto.html)

### MAUI Blazor Hybrid App
This project utilizes the Image Processor project for image processing. This will connect to the ESP32 device, take 4 images with the smartphone's camera, composite those images into a normal map, and then run the ONNX model on them. This project uses the MAUI Blazor Hybrid template for cross-platform support.

#### Helpful Guides
The following guide is useful for connecting the mobile app to the ESP32 via Bluetooth.
- [MAUI Bluetooth](https://github.com/dotnet-bluetooth-le/dotnet-bluetooth-le)

### Requirements
- .NET 9 SDK
- MAUI workloads

#### Helpful Guides
The following guide is useful for installing MAUI.
- [Installing Maui](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation?view=net-maui-9.0&tabs=visual-studio)


## ONNX Model
An ONNX model is a machine learning model saved in the Open Neural Network Exchange format, which allows different frameworks to share and run it. We use it for use in the [mobile app](#maui-blazor-hybrid-app). Visit the path `BaseballCardGrader/Trainer` for the code relevant to this section.

### Annotating Images
The model assumes only normal images are used for training. These normal images can be extracted from the mobile app. They can then be annotated with a program like LabelMe. If using LabelMe, ensure "Save With Image Data" is not toggled on. 

Newly annotated images can be stored in `BaseballCardGrader/Trainer/dataset`.

#### Links
Links to install LabelMe.
- [LabelMe GitHub](https://github.com/wkentaro/labelme)

### Training The Model
In the `BaseballCardGrader/Trainer` directory, run the following command.
```
docker compose up
```
This will create a PyTorch container with CUDA support, copy the relevant python files over, install any dependencies, map the dataset and checkpoint folders to the container, run the script to create the model, and then export the script to the ONNX format.

The ONNX model will be stored in the path `BaseballCardGrader/Trainer/checkpoint` after training.

#### Requirements:
- Nvidia GPU that supports CUDA
- Docker Desktop
- Windows 11 with WSL2 enabled

### Helpful Guides
The following guides are helpful for training and exporting model.
- [Train Model](https://christianjmills.com/posts/pytorch-train-mask-rcnn-tutorial/)
- [Export To ONNX](https://christianjmills.com/posts/pytorch-train-mask-rcnn-tutorial/onnx-export/)

## Box
### 3D Print
The files in `BaseballCardGrader/Device/cad` can be used to 3D print the box.

#### Caveats
- There is no easy way to attach the lid to the box. An easy solution is to cut the socket on the right side of the box in half. That way the lid just sits on top.
- The indent in the middle of the box is a design flaw. The "Box Fix" can be used to fill in the indent.

### ESP32
An ESP32 is a low-cost, low-power microcontroller with built-in Wi-Fi and Bluetooth capabilities. For our case, we use one to handle the LED lights and connect to the mobile app. Visit the Path: `BaseballCardGrader/Device/esp32` for the code relevant to this section. 

#### Components
<img height="600" alt="circuit_image" src="https://github.com/user-attachments/assets/09fe7c6b-dc51-40a6-8c4e-98718ccee222" />

The following components were used.
- Seeed Studio XIAO ESP32-C3
- 4 White 6500k LEDS
- 1 Button
- Battery (Optional)
  
##### Buy links
- [ESP32 Amazon Link](https://www.amazon.com/dp/B0B94JZ2YF?ref=ppx_yo2ov_dt_b_fed_asin_title&th=1)
- [White 6500k LEDS Amazon Link](https://www.amazon.com/dp/B01DBZICDC?ref=ppx_yo2ov_dt_b_fed_asin_title&th=1)
- [Button Amazon Link](https://www.amazon.com/DIYables-Button-Arduino-ESP8266-Raspberry/dp/B0BXKN4TY6/ref=sr_1_18?crid=258HRS7XS9CT7&dib=eyJ2IjoiMSJ9.Z4-eK95_kbJEqwzn80rsWepUtV4jC-jVZE-MVroIqXo7-uw-_u4Kv1dzVcC64hZzVKQgVBBBNnFiw8Pi55KNM8FxNB7xu5pO2fL1kVOeomEGE8ZqQJWCKDfFqDB7FL8uCps-EaQKrb1F80nJq1RSsBmClKEQnKvHoAArC_6PD0OKpMiKtTDy6yYm-EevdZtNxpXVTxx6bYT-xN9zmGqhfURB8d05qxq3SaYxM-kTSkY.H5SbX2x5z9RT1E4KgrtlWIYbXZUC1bTGnQWqQn-X0T8&dib_tag=se&keywords=arduino%2Bbutton&qid=1758793532&sprefix=arduion%2Bbutto%2Caps%2C179&sr=8-18&th=1)
- [Battery Amazon Link](https://www.amazon.com/dp/B08T6GT7DV?ref=ppx_yo2ov_dt_b_fed_asin_title)

#### Updating Firmware
PlatformIO is used over the Arduino IDE for better version control support. It can be used to upload newer firmware to the ESP32 device.

##### Helpful Guides
The following guides are helpful for learning PlatformIO and how to code for ESP32.
- [PlatformIO Tutorial](https://www.youtube.com/watch?v=QMYhVqjBhKQ&t=693s)
- [ESP32 Guide](https://wiki.seeedstudio.com/XIAO_ESP32C3_Getting_Started/)

##### Requirements
- Visual Studio Code
- PlatformIO extension
