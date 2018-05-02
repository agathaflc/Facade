# imports
import pickle
import urllib
import json, base64, io
from flask import Flask, jsonify, request, Response, redirect, url_for, abort, json
from load_model_emotion import eval_model, load_model
from predict_emotion import predict_emotion, processImage, predict_for_kate, new_predict_for_kate
from pprint import pprint
from imageio import imread

emotion_labels = {0:'neutral',1:'anger',2:'fear',3:'happy',4:'sad',5:'surprise'}

app = Flask(__name__)


@app.route('/model_1/emotion_detection', methods=['POST'])
def new_predict():
    
    print("beginning emotion_detection")
    data = request.data
    json_data = json.loads(data)
    # print json_data
    if not data or not 'data' in json_data:
        abort(400)
    
    if not 'image' in json_data["data"]:
        abort(400)
    
    if not 'sessionId' in json_data["data"]:
        abort(400)

    print("passed inspection")

    base64Image = json_data["data"]["image"]  # in base64
    print(base64Image)
    # sessionId = json_data["data"]["sessionId"]
    buf = base64.b64decode(base64Image)
    
    
    # Reconstruct image as an numpy array
    image = imread(io.BytesIO(buf))
    pred = new_predict_for_kate(image, loadmodel)
    
    return jsonify(pred)

@app.route('/model_1/predict/<string:prediction>', methods=['POST'])
def predict(prediction):
    
    pred = predict_for_kate(prediction, loadmodel)
    print (pred)
    
    return jsonify(pred)


@app.route('/')
def home(prediction=None):
    return ("FACE EMOTION MODEL IS RUNNING")


if __name__ == '__main__':
    print 'load-initialize model...'
    loadmodel = load_model("saved_models/deployment/model.json", "saved_models/deployment/best_weights.hd5")
    app.run(debug=False)
