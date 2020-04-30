package com.frozenwater.imageclassifierplugin;


import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.os.SystemClock;
import android.os.Trace;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import org.tensorflow.lite.DataType;
import org.tensorflow.lite.Interpreter;
import org.tensorflow.lite.support.common.FileUtil;
import org.tensorflow.lite.support.common.TensorOperator;
import org.tensorflow.lite.support.common.TensorProcessor;
import org.tensorflow.lite.support.common.ops.NormalizeOp;
import org.tensorflow.lite.support.image.ImageProcessor;
import org.tensorflow.lite.support.image.TensorImage;
import org.tensorflow.lite.support.image.ops.ResizeOp;
import org.tensorflow.lite.support.image.ops.ResizeWithCropOrPadOp;
import org.tensorflow.lite.support.image.ops.Rot90Op;
import org.tensorflow.lite.support.label.TensorLabel;
import org.tensorflow.lite.support.tensorbuffer.TensorBuffer;

import java.io.File;
import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.MappedByteBuffer;
import java.util.Collections;
import java.util.List;
import java.util.Map;

public class ImageClassifier {

    /** Number of results to show in the UI. */
    private static final int MAX_RESULTS = 3;
    /** The loaded TensorFlow Lite model. */
    private MappedByteBuffer tfliteModel;
    /** Image size along the x axis. */
    private int imageSizeX;
    /** Image size along the y axis. */
    private int imageSizeY;
    /** An instance of the driver class to run model inference with Tensorflow Lite. */
    protected Interpreter tflite;
    /** Options for configuring the Interpreter. */
    private final Interpreter.Options tfliteOptions = new Interpreter.Options();
    /** Labels corresponding to the output of the vision model. */
    private List<String> labels;
    /** Input image TensorBuffer. */
    private TensorImage inputImageBuffer;
    /** Output probability TensorBuffer. */
    private TensorBuffer outputProbabilityBuffer;
    /** Processer to apply post processing of the output probability. */
    private TensorProcessor probabilityProcessor;


    /***
     * Setup interpreter and initliaze input and output tensors to run
     * cassifier later.
     * @param numThreads number of threads to be used by interpretr
     * @throws IOException
     */
    protected void initializeInterpreter(int numThreads) throws IOException {
        tfliteModel = FileUtil.loadMappedFile(UnityPlayer.currentActivity, getModelPath());

        //Set interpreter
        tfliteOptions.setNumThreads(numThreads);
        tflite = new Interpreter(tfliteModel, tfliteOptions);

        // Loads labels out from the label file.
        labels = FileUtil.loadLabels(UnityPlayer.currentActivity, getLabelPath());

        //Set Input and Output shapes
        int imageTensorIndex = 0;
        int[] imageShape = tflite.getInputTensor(imageTensorIndex).shape(); // {1, height, width, 3}
        imageSizeY = imageShape[1];
        imageSizeX = imageShape[2];
        DataType imageDataType = tflite.getInputTensor(imageTensorIndex).dataType();
        int probabilityTensorIndex = 0;
        int[] probabilityShape =
                tflite.getOutputTensor(probabilityTensorIndex).shape(); // {1, NUM_CLASSES}
        DataType probabilityDataType = tflite.getOutputTensor(probabilityTensorIndex).dataType();

        // Creates the input tensor.
        inputImageBuffer = new TensorImage(imageDataType);

        // Creates the output tensor and its processor.
        outputProbabilityBuffer = TensorBuffer.createFixedSize(probabilityShape, probabilityDataType);

        // Creates the post processor for the output probability.
        probabilityProcessor = new TensorProcessor.Builder().add(getPostprocessNormalizeOp()).build();

        Log.d("Unity", "Created a Tensorflow Lite Image Classifier.");
    }

    /** Loads input image, and applies preprocessing. */
    private TensorImage loadImage(final Bitmap bitmap, int sensorOrientation) {
        // Loads bitmap into a TensorImage.
        inputImageBuffer.load(bitmap);

        // Creates processor for the TensorImage.
        int cropSize = Math.min(bitmap.getWidth(), bitmap.getHeight());
        int numRotation = sensorOrientation / 90;
        // Define an ImageProcessor from TFLite Support Library to do preprocessing
        ImageProcessor imageProcessor =
                new ImageProcessor.Builder()
                        .add(new ResizeWithCropOrPadOp(cropSize, cropSize))
                        .add(new ResizeOp(imageSizeX, imageSizeY, ResizeOp.ResizeMethod.NEAREST_NEIGHBOR))
                        .add(new Rot90Op(numRotation))
                        .add(getPreprocessNormalizeOp())
                        .build();
        return imageProcessor.process(inputImageBuffer);
    }


    /***
     * Run Classifier on the bitmap provided and return the label with max probability
     * @param bitmap Bitmap to run classifier on
     * @return label of probability with highest probability in distribution.
     */
    public String recognizeImage(final Bitmap bitmap) {
        // Logs this method so that it can be analyzed with systrace.
        Trace.beginSection("recognizeImage");

        Trace.beginSection("loadImage");
        long startTimeForLoadImage = SystemClock.uptimeMillis();
        int sensorOrientation = 90;
        inputImageBuffer = loadImage(bitmap, sensorOrientation);
        long endTimeForLoadImage = SystemClock.uptimeMillis();
        Trace.endSection();
        Log.d("Unity", "Timecost to load the image: " + (endTimeForLoadImage - startTimeForLoadImage));

        // Runs the inference call.
        Trace.beginSection("runInference");
        long startTimeForReference = SystemClock.uptimeMillis();
        // Run TFLite inference
        tflite.run(inputImageBuffer.getBuffer(), outputProbabilityBuffer.getBuffer().rewind());
        long endTimeForReference = SystemClock.uptimeMillis();
        Trace.endSection();
        Log.d("Unity", "Timecost to run model inference: " + (endTimeForReference - startTimeForReference));

        // Gets the map of label and probability.
        // Use TensorLabel from TFLite Support Library to associate the probabilities
        //       with category labels
        Map<String, Float> labeledProbability =
                new TensorLabel(labels, probabilityProcessor.process(outputProbabilityBuffer))
                        .getMapWithFloatValue();

        String maxLabel = "";
        // get max value in labelProbability HashMap
        float maxValueInMap =(Collections.max(labeledProbability.values()));
        // Iterate through HashMap
        for (Map.Entry<String, Float> entry : labeledProbability.entrySet()) {
            if (entry.getValue()==maxValueInMap) {
                maxLabel = entry.getKey();
            }
        }
        return maxLabel;
    }


    /**
     * Decode the image file saved by Unity Wrapper
     * and call the classifier on the image
     * @param filePath path where image is saved
     * @return Label of max probability in output distribution.
     */
    public String callClassifier(String filePath){

        Bitmap bitmap = BitmapFactory.decodeFile(filePath);

        return recognizeImage(bitmap);
    }


    /**
     * Close interpreter
     */
    public void close() {
        if (tflite != null) {

            tflite.close();
            tflite = null;
        }

        tfliteModel = null;
    }


    /** GETTERS **/
    private String getModelPath() {
        return "model.tflite";
    }

    private String getLabelPath() {
        return "labels.txt";
    }

    private TensorOperator getPreprocessNormalizeOp() {
        return new NormalizeOp(127.5f, 127.5f);
    }

    private TensorOperator getPostprocessNormalizeOp() {
        return new NormalizeOp(0.0f, 1.0f);
    }
}


