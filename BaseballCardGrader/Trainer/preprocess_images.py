import os
import sys
import numpy as np
import cv2
from PIL import Image
import pillow_heif
import re
from collections import defaultdict

requiredKeywords = ["Left", "Right", "Up", "Down"]

def readImageAsGrayscale(path):
    ext = os.path.splitext(path)[1].lower()
    if ext == ".heic":
        heif_file = pillow_heif.read_heif(path)
        image = Image.frombytes(
            heif_file.mode, heif_file.size, heif_file.data,
            "raw"
        ).convert("L")
        return np.array(image)
    else:
        return cv2.imread(path, cv2.IMREAD_GRAYSCALE)

def findPictureFiles(directoryPath):
    foundFiles = {}

    if not os.path.exists(directoryPath):
        raise FileNotFoundError(f"Directory {directoryPath} does not exist.")

    for filename in os.listdir(directoryPath):
        lowered = filename.lower()
        for key in requiredKeywords:
            if key in lowered and filename.lower().endswith(('.png', '.jpg', '.jpeg', '.heic')):
                foundFiles[key] = os.path.join(directoryPath, filename)

    if set(foundFiles.keys()) != set(requiredKeywords):
        missing = [key for key in requiredKeywords if key not in foundFiles]
        raise ValueError(f"Missing required images: {missing}")

    nameOfPicture = os.path.basename(os.path.normpath(directoryPath))
    return {nameOfPicture: foundFiles}

def getGreyscaledImageList(fileMappings):
    return [
        readImageAsGrayscale(fileMappings["Right"]),
        readImageAsGrayscale(fileMappings["Left"]),
        readImageAsGrayscale(fileMappings["Up"]),
        readImageAsGrayscale(fileMappings["Down"]),
    ]

def overlayImages(image1, image2):
    return cv2.addWeighted(image1, 0.5, image2, 0.5, 0)

def groupFlatHeicFiles(batchDir):
    grouped = defaultdict(dict)
    pattern = re.compile(r"(.+?_\d*)_(up|down|left|right)\.HEIC$", re.IGNORECASE)

    for filename in os.listdir(batchDir):
        match = pattern.match(filename)
        if match:
            baseName, direction = match.groups()
            key = baseName  # This will be used as the output file name
            grouped[key][direction] = os.path.join(batchDir, filename)

    validGroups = {}
    for name, files in grouped.items():
        if set(files.keys()) == set(requiredKeywords):
            validGroups[name] = files
        else:
            missing = [k for k in requiredKeywords if k not in files]
            print(f"Skipping {name}: missing views {missing}")

    return validGroups

def generateOverlayedImage(greyScaledImages, mode, blueValue=0):
    height, width = greyScaledImages[0].shape
    aboveLeftPicture = np.zeros((height, width, 3), dtype=np.uint8)
    bottomRightPicture = np.zeros((height, width, 3), dtype=np.uint8)

    aboveLeftPicture[:, :, 0] = blueValue
    bottomRightPicture[:, :, 0] = blueValue

    if mode == "overlay":
        aboveLeftPicture[:, :, 1] = greyScaledImages[2]  # up
        aboveLeftPicture[:, :, 2] = greyScaledImages[1]  # left
        bottomRightPicture[:, :, 1] = greyScaledImages[3]  # down
        bottomRightPicture[:, :, 2] = greyScaledImages[0]  # right
    elif mode == "normalMap":
        aboveLeftPicture[:, :, 1] = cv2.normalize(greyScaledImages[2], None, 0, 127, cv2.NORM_MINMAX)  # up
        aboveLeftPicture[:, :, 2] = cv2.normalize(greyScaledImages[1], None, 0, 127, cv2.NORM_MINMAX)  # left
        bottomRightPicture[:, :, 1] = cv2.normalize(greyScaledImages[3], None, 128, 255, cv2.NORM_MINMAX)  # down
        bottomRightPicture[:, :, 2] = cv2.normalize(greyScaledImages[0], None, 128, 255, cv2.NORM_MINMAX)  # right

    return overlayImages(aboveLeftPicture, bottomRightPicture)

def processImageDir(imageDir, outputDir, mode, outputName):
    directoryOfPictures = findPictureFiles(imageDir)
    _, fileMappings = list(directoryOfPictures.items())[0]
    greyScaledImages = getGreyscaledImageList(fileMappings)

    overlayedImage = generateOverlayedImage(greyScaledImages, mode)
    outputPath = os.path.join(outputDir, outputName)
    cv2.imwrite(outputPath, overlayedImage)

def runBatchMode(batchDir, outputDir, mode, _defectNameIgnored):
    groupedFiles = groupFlatHeicFiles(batchDir)

    for baseName, fileMappings in groupedFiles.items():
        try:
            outputName = f"{baseName}.png"
            greyScaledImages = getGreyscaledImageList(fileMappings)
            overlayedImage = generateOverlayedImage(greyScaledImages, mode)
            outputPath = os.path.join(outputDir, outputName)
            cv2.imwrite(outputPath, overlayedImage)
            print(f"Processed {outputName}")
        except Exception as e:
            print(f"Error processing {baseName}: {e}")

if __name__ == "__main__":
    args = sys.argv[1:]

    if len(args) < 3:
        print("Usage:")
        print("  Batch Mode:  python preprocess_images.py -batch <batchDir> <resultsDir> <normalMap | overlay> <defectName>")
        print("  Single Mode: python preprocess_images.py <imageDir> <resultsDir> <normalMap | overlay>")
        sys.exit(1)

    if args[0] == "-batch":
        if len(args) != 5:
            print("Usage: python preprocess_images.py -batch <batchDir> <resultsDir> <normalMap | overlay> <defectName>")
            sys.exit(1)
        batchDir = args[1]
        resultsDir = args[2]
        mode = args[3]
        defectName = args[4]
        runBatchMode(batchDir, resultsDir, mode, defectName)
    else:
        imageDir = args[0]
        resultsDir = args[1]
        mode = args[2]
        try:
            baseName = os.path.basename(imageDir)
            outputName = f"{baseName}.{mode}.png"
            processImageDir(imageDir, resultsDir, mode, outputName)
            print("Done.")
        except Exception as e:
            print(f"Failed to process image directory: {e}")
