FROM pytorch/pytorch:2.7.0-cuda12.8-cudnn9-runtime
WORKDIR /app
COPY ./train_model.py ./train_model.py
COPY ./convert_to_onnx.py ./convert_to_onnx.py
COPY ./requirements.txt ./requirements.txt
RUN pip install --no-cache-dir -r requirements.txt