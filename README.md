### Natural-AI-Mixed-Reality

# Unity

Unity project implemented with Multimodal Llama 3.2, hands interaction, and Wit AI to get transcriptions.

## Robot

The robot included in the sample scene has very basic behaviors.

![robot.png](https://github.com/bDiazTaima/Natural-AI-Mixed-Reality/blob/main/Assets/GitImages/robot.png)

### Robot.cs

It has two behaviors: the first is a LookAt, which makes the robot always look at the target, in this case, the central camera position of the headset.

The second behavior is FollowTarget, which makes the robot always stay positioned in front of the player while maintaining a certain offset from the position of an empty object (Robot_Target).

```jsx
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Robot : MonoBehaviour
{
    [SerializeField] private Transform robot_target;
    [SerializeField] private Transform player;
    [SerializeField] private float y_position;

    private void Update()
    {
        transform.LookAt(player);
        Vector3 target_position = new Vector3(robot_target.position.x, robot_target.position.y, robot_target.position.z);
        Vector3 new_location = Vector3.Lerp(transform.position, target_position, Time.deltaTime);
        transform.position = new_location;
    }
}
```

### Expression.cs

This script is included in the object containing the robot's face mesh (Robot_Screen). It helps provide visual feedback to the user, indicating in which phase the robot is in during the analysis and response process of the user's prompt by changing the emission color property of the material.

```jsx
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Expression : MonoBehaviour
{
    static public Expression Instance { get; private set; }
    private Material material;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        material = renderer.material;
        SetColor(Color.red);
    }

    public void SetColor(Color color)
    {
        material.SetColor("_EmissionColor", color);
    }
}

```

## Wit.AI

For the transcription part, [wit.ai](https://wit.ai/) is used. I include this [repository](https://github.com/wit-ai/wit-unity/blob/main/Tutorials/ShapesTutorial.md) which has an example of how to use Wit with Unity through the WitConfig object.

### **Transcription to TextMeshPro text**

The EventHandler object is found in WitDictation.

![transcription.png](https://github.com/bDiazTaima/Natural-AI-Mixed-Reality/blob/main/Assets/GitImages/transcription.png)

It has a component called "MultiRequestTranscription". In the event list, it has a dynamic text that automatically writes the transcription received from the voice on this object.

![multirequest.png](https://github.com/bDiazTaima/Natural-AI-Mixed-Reality/blob/main/Assets/GitImages/multirequest.png)

### WitActivation.cs

Since it's not recommended for the headset's microphone to always be listening to the user, the Wit component deactivates activation after a certain amount of time.

This script has a function (ToggleActivation) that allows the microphone to be turned back on, as well as providing visual feedback with a blue color on the robot, indicating that it is listening. When the microphone is turned off, the robot's color changes to red.

```jsx
using JetBrains.Annotations;
using Meta.WitAi;
using Meta.WitAi.Dictation;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

public class WitActivation : MonoBehaviour
{
    [SerializeField] private Wit wit;
    public bool active = false;
    [SerializeField] TextMeshPro t, t2;
    [SerializeField] DictationService service;
    [SerializeField] PostRequest post_request;
    private bool restart = true;

    private void Start()
    {
        ToggleActivation();
    }

        private void OnValidate()
    {
        if (!wit) wit = GetComponent<Wit>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleActivation();
        }
        t.text = wit.Active.ToString();
        t2.text = service.Active.ToString();

        if (wit.Active == false)
        {
            if (restart)
            {
                post_request.SetInputText();
                Expression.Instance.SetColor(Color.yellow);
                restart = false;
            }
        }
    }

    public void ToggleActivation()
    {
        active = !active;
        if (active)
        {
            restart = true;
            wit.Activate();
            service.Activate();
            Expression.Instance.SetColor(Color.blue);
        }
        else
        {
            Expression.Instance.SetColor(Color.red);
            wit.Deactivate();
            service.Deactivate();
        }
    }
}
```

Finally, in the (Update) function, this script contains a condition that, when the microphone turns off after being activated, triggers the behavior that calls the Llama API (`post_request.SetInputText()`).

## API Llama

### PostRequest.cs

This file makes a call to the Llama API using an IEnumerator function (`UploadFormData()`) that is called when the headset's microphone is turned off. This function creates a `WWWForm` that includes the prompt from the transcription made by Wit and also receives an image activated by a hand gesture based on what the user is seeing. These two inputs are sent as a prompt to the multimodal model to provide a response from the LLM.

```jsx
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using TMPro;

public class PostRequest : MonoBehaviour
{
    public string url;
    public string inputText;
    public Texture2D image;
    public string separatorWord;
    [SerializeField] TextMeshPro transcription_txt;
    [SerializeField] TextMeshPro answer_txt;

    public void Start()
    {
        inputText = string.Empty;
        answer_txt.text = string.Empty;
        //StartCoroutine(UploadFormData());
    }

    public void SetInputText()
    {
        answer_txt.text = string.Empty;
        inputText += transcription_txt.text;
        StartCoroutine(UploadFormData());
    }

    //////////////////////////////////

    string RemoveNewLines(string inputText)
    {
        if (string.IsNullOrEmpty(inputText))
        {
            Debug.LogWarning("El texto de entrada está vacío o es nulo.");
            return string.Empty;
        }

        // Reemplazar "\n" con un string vacío
        string processedText = inputText.Replace("\n", "");
        return processedText;
    }

    //////////////////////////////////

    IEnumerator UploadFormData()
    {
        // Convert texture to uncompressed format if necessary
        Texture2D uncompressedTexture = ConvertToUncompressed(image);

        // Encode the texture
        byte[] imageBytes = uncompressedTexture.EncodeToPNG();

        // Create a form and add fields
        WWWForm form = new WWWForm();
        form.AddField("input_text", inputText);
        form.AddBinaryData("image", imageBytes, "image.png", "image/png");

        UnityWebRequest www = new UnityWebRequest(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(form.data),
            downloadHandler = new DownloadHandlerBuffer()
        };

        // Set headers
        www.SetRequestHeader("Content-Type", form.headers["Content-Type"]);
        www.SetRequestHeader("ngrok-skip-browser-warning", "69420");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + www.error);
        }
        else
        {
            string jsonResponse = www.downloadHandler.text;
            string answer = GetSubstringAfterWord(jsonResponse, separatorWord);

            answer = RemoveNewLines(answer);

            answer_txt.text = answer;
            Expression.Instance.SetColor(Color.green);
        }
    }

    Texture2D ConvertToUncompressed(Texture2D texture)
    {
        Texture2D newTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        newTexture.SetPixels(texture.GetPixels());
        newTexture.Apply();
        return newTexture;
    }

    string GetSubstringAfterWord(string fullString, string word)
    {
        // Find the index of the separator word
        int index = fullString.IndexOf(word);

        // Check if the word exists in the string
        if (index != -1)
        {
            // Get the substring after the word
            return fullString.Substring(index + word.Length + 4);
        }
        else
        {
            // Return an empty string or the original string if the word is not found
            return string.Empty;
        }
    }
}
```

### Capture

The capture event is triggered by recognizing if the right and left hand fingers are in a specific position.

![capture.png](https://github.com/bDiazTaima/Natural-AI-Mixed-Reality/blob/main/Assets/GitImages/capture.png)

When these positions are met

![capture2.png](https://github.com/bDiazTaima/Natural-AI-Mixed-Reality/blob/main/Assets/GitImages/capture2.png)

 the `TriggerSnapshot` event is called through the (SelectorUnityEventWrapper) script.

![capture3.png](https://github.com/bDiazTaima/Natural-AI-Mixed-Reality/blob/main/Assets/GitImages/capture3.png)

The image obtained from the screenshot is sent to the (PostProcess.cs) script to be evaluated in the prompt.

```jsx
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using System;

namespace Anaglyph.DisplayCapture
{
    [DefaultExecutionOrder(-1000)]
    public class VRDisplayCaptureManager : MonoBehaviour
    {
        public static VRDisplayCaptureManager Instance { get; private set; }

        [SerializeField] private Vector2Int textureSize = new(1024, 1024);
        [SerializeField] private Vector2Int cropSize = new(512, 512); // Size of the cropped region

        [Header("Trigger Settings")]
        [SerializeField] private bool useExternalTrigger = false;
        [SerializeField] private UnityEvent onCaptureTriggered;
        [SerializeField] private UnityEvent<Texture2D> onSnapshotTaken;

        [Header("Controller Settings")]
        [SerializeField] private XRNode controllerNode = XRNode.RightHand;
        [SerializeField] private InputFeatureUsage<bool> selectedButton = CommonUsages.primaryButton;
        [Tooltip("Choose which controller to use")]
        public ControllerHand controllerHand = ControllerHand.Right;
        [Tooltip("Choose which button to use")]
        public ControllerButton buttonChoice = ControllerButton.TriggerButton;

        [Header("Mesh Settings")]
        [SerializeField] private MeshRenderer targetMeshRenderer;
        [SerializeField] private int materialIndex = 0;

        [Header("Post Request Settings")]
        [SerializeField] private PostRequest postRequest;

        public Texture2D SnapshotTexture => croppedTexture;

        public enum ControllerHand
        {
            Left,
            Right
        }

        public enum ControllerButton
        {
            TriggerButton,
            GripButton,
            PrimaryButton,
            SecondaryButton,
            MenuButton,
            Primary2DAxisClick,
            PrimaryTouch,
            SecondaryTouch
        }

        private Texture2D screenTexture;
        private Texture2D snapshotTexture;
        private Texture2D croppedTexture;
        private bool wasButtonPressed = false;
        private AndroidInterface androidInterface;
        private unsafe sbyte* imageData;
        private int bufferSize;

        // Nested AndroidInterface class - this is important!
        private class AndroidInterface
        {
            private AndroidJavaClass androidClass;
            private AndroidJavaObject androidInstance;

            public AndroidInterface(GameObject messageReceiver, int textureWidth, int textureHeight)
            {
                androidClass = new AndroidJavaClass("com.trev3d.DisplayCapture.DisplayCaptureManager");
                androidInstance = androidClass.CallStatic<AndroidJavaObject>("getInstance");
                androidInstance.Call("setup", messageReceiver.name, textureWidth, textureHeight);
            }

            public void RequestCapture() => androidInstance.Call("requestCapture");
            public void StopCapture() => androidInstance.Call("stopCapture");

            public unsafe sbyte* GetByteBuffer()
            {
                AndroidJavaObject byteBuffer = androidInstance.Call<AndroidJavaObject>("getByteBuffer");
                return AndroidJNI.GetDirectBufferAddress(byteBuffer.GetRawObject());
            }
        }

        private InputFeatureUsage<bool> GetSelectedInputFeature()
        {
            switch (buttonChoice)
            {
                case ControllerButton.TriggerButton:
                    return CommonUsages.triggerButton;
                case ControllerButton.GripButton:
                    return CommonUsages.gripButton;
                case ControllerButton.PrimaryButton:
                    return CommonUsages.primaryButton;
                case ControllerButton.SecondaryButton:
                    return CommonUsages.secondaryButton;
                case ControllerButton.MenuButton:
                    return CommonUsages.menuButton;
                case ControllerButton.Primary2DAxisClick:
                    return CommonUsages.primary2DAxisClick;
                case ControllerButton.PrimaryTouch:
                    return CommonUsages.primaryTouch;
                case ControllerButton.SecondaryTouch:
                    return CommonUsages.secondaryTouch;
                default:
                    return CommonUsages.triggerButton;
            }
        }

        private void Awake()
        {
            Instance = this;
            androidInterface = new AndroidInterface(gameObject, textureSize.x, textureSize.y);
            screenTexture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, 1, false);
            snapshotTexture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, 1, false);
            croppedTexture = new Texture2D(cropSize.x * 2, cropSize.y * 2, TextureFormat.RGBA32, 1, false);
            bufferSize = textureSize.x * textureSize.y * 4;

            controllerNode = controllerHand == ControllerHand.Right ? XRNode.RightHand : XRNode.LeftHand;

            if (onCaptureTriggered == null)
                onCaptureTriggered = new UnityEvent();
            if (onSnapshotTaken == null)
                onSnapshotTaken = new UnityEvent<Texture2D>();
        }

        private void Start()
        {
            if (!targetMeshRenderer)
            {
                Debug.LogError("Target MeshRenderer not assigned!");
                return;
            }

            if (postRequest == null)
            {
                postRequest = FindObjectOfType<PostRequest>();
                if (postRequest == null)
                {
                    Debug.LogWarning("PostRequest component not found in the scene!");
                }
            }

            StartScreenCapture();
        }

        private void Update()
        {
            if (!useExternalTrigger)
            {
                CheckControllerInput();
            }
        }

        private void CheckControllerInput()
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(controllerNode);
            bool isPressed = false;

            if (device.isValid)
            {
                if (device.TryGetFeatureValue(GetSelectedInputFeature(), out isPressed))
                {
                    if (isPressed && !wasButtonPressed)
                    {
                        TakeSnapshot();
                    }
                    wasButtonPressed = isPressed;
                }
            }
        }

        public void TriggerSnapshot()
        {
            if (useExternalTrigger)
            {
                TakeSnapshot();
                onCaptureTriggered.Invoke();
            }
        }

        private void TakeSnapshot()
        {
            Graphics.CopyTexture(screenTexture, snapshotTexture);

            // Calculate the center region to crop
            int startX = (textureSize.x - cropSize.x) / 2;
            int startY = (textureSize.y - cropSize.y) / 2;

            // Get the pixels from the center region
            Color[] cropPixels = snapshotTexture.GetPixels(startX, startY, cropSize.x, cropSize.y);

            // Scale up the cropped pixels
            Color[] scaledPixels = new Color[cropSize.x * 2 * cropSize.y * 2];

            for (int y = 0; y < cropSize.y * 2; y++)
            {
                for (int x = 0; x < cropSize.x * 2; x++)
                {
                    // Sample from the original cropped pixels
                    int sourceX = x / 2;
                    int sourceY = y / 2;
                    int sourceIndex = sourceY * cropSize.x + sourceX;
                    int targetIndex = y * (cropSize.x * 2) + x;

                    scaledPixels[targetIndex] = cropPixels[sourceIndex];
                }
            }

            // Apply the scaled pixels to the cropped texture
            croppedTexture.SetPixels(0, 0, cropSize.x * 2, cropSize.y * 2, scaledPixels);
            croppedTexture.Apply();

            ApplySnapshotToMesh();

            if (postRequest != null)
            {
                postRequest.image = croppedTexture;
            }

            onSnapshotTaken.Invoke(croppedTexture);

            Debug.Log($"Snapshot taken and cropped {(useExternalTrigger ? "via external trigger" : $"using {controllerHand} controller {buttonChoice}")}!");
        }

        private void ApplySnapshotToMesh()
        {
            if (targetMeshRenderer && targetMeshRenderer.materials.Length > materialIndex)
            {
                Material material = targetMeshRenderer.materials[materialIndex];
                material.mainTexture = croppedTexture;
                targetMeshRenderer.materials[materialIndex] = material;
            }
        }

        public void StartScreenCapture()
        {
            androidInterface.RequestCapture();
        }

        public void StopScreenCapture()
        {
            androidInterface.StopCapture();
        }

        private unsafe void OnCaptureStarted()
        {
            imageData = androidInterface.GetByteBuffer();
        }

        private unsafe void OnNewFrameAvailable()
        {
            if (imageData == default) return;
            screenTexture.LoadRawTextureData((IntPtr)imageData, bufferSize);
            screenTexture.Apply();
        }
    }
}
```

The capture logic was sourced from the repository [trev3d / QuestDisplayAccessDemo](https://github.com/trev3d/QuestDisplayAccessDemo).

## Commands

This repository includes the implementation of commands via [Wit.ai](http://wit.ai/), which can be tested using the hidden object (Shapes) found in the Hierarchy. If you give a command such as "make capsule blue" [you can substitute any shape with those available in the object and also any color], you will see that the geometry changes to the chosen color.

![commands.png](https://github.com/bDiazTaima/Natural-AI-Mixed-Reality/blob/main/Assets/GitImages/commands.png)

## RoadMap

- Create an interface to easily add trained commands and activate custom events
- Enable voice activation for AI responses
- Integration of Llama Guard
- Add a version that can run directly On Device without needing an external API call

## References

- https://github.com/trev3d/QuestDisplayAccessDemo
- https://wit.ai/docs/tutorials#wit-unity-shapes-tutorial
- https://wit.ai/

# Training Model Backend

# Model Training Backend Documentation

This backend system enables training of a **LLaMA 3.2 11B Vision-Instruct Model** on a dataset derived from PDF files and art-related images. The process is managed via a **Node.js server** running on an **Ubuntu virtual environment** within **Windows 11**. Below are the key components and steps:

---

## **System Components**

### 1. **Node.js Server** (`mai-base-server_v2.js`)
   - Handles:
     - PDF upload and storage.
     - Dataset creation and optimization.
     - Training model execution.
   - Key API Endpoints:
     - `/receivepdfandtrain` - Upload and preprocess PDFs.
     - `/convertData` - Optimize dataset using Python.
     - `/trainModel` - Train the model.

### 2. **Python Scripts**
   - `convertData.py`:
     - Converts preprocessed PDF content into a structured dataset in JSON format.
   - `trainModel.py`:
     - Fine-tunes the LLaMA model using the optimized dataset and art-related images.

### 3. **Frontend Interfaces**
   - `upload_pdf_v2.html`: Upload PDFs and specify the model name.
   - `train_models_v2.html`: Optimize dataset and initiate model training.

---

## **Backend Workflow**

### 1. **Upload PDFs** (`upload_pdf_v2.html` + `/receivepdfandtrain`)
   - User uploads PDFs and specifies a model name.
   - PDFs are renamed sequentially and stored in `/pdf_usr_uploads/<model_name>/`.
   - A preprocessing command generates preliminary data from PDFs.

   ```javascript
   app.post('/receivepdfandtrain', upload.array('pdfs'), (req, res) => {
       const command = `marker "${uploadDir}" "${dataDir}" --workers 4 --max 10`;
       exec(command, ...);  // Preprocess PDFs
   });
   ```

### 2. **Optimize Dataset** (`train_models_v2.html` + `/convertData`)
   - Python script processes PDFs and generates a JSON dataset with user prompts and assistant responses.

   ```python
   # convertData.py
   dataset.append({"message": tmp2})
   with open(output_file, "w", encoding="utf-8") as f:
       json.dump(dataset, f, indent=4, ensure_ascii=False)
   ```

### 3. **Fine-Tune Model** (`train_models_v2.html` + `/trainModel`)
   - Combines the dataset with art images for model fine-tuning.
   - Uses the `transformers` library with configuration optimizations like LoRA and bfloat16 precision.

   ```python
   # trainModel.py
   trainer.train()  # Fine-tuning step with processed dataset and images
   ```

### 4. **Frontend Communication**
   - Interfaces like `upload_pdf_v2.html` and `train_models_v2.html` use local storage and fetch API for seamless interaction with backend endpoints.

---

## **File Structure**

```plaintext
project/
├── mai-base-server_v2.js      # Node.js backend
├── convertData.py             # Dataset optimization
├── trainModel.py              # Model training
├── pdf_usr_uploads/           # Uploaded PDFs and datasets
│   ├── <model_name>/
│   │   ├── pdf_1.pdf
│   │   ├── pdf_2.pdf
│   │   ├── <model_name>_dataset.json
├── training_set/              # Art images
│   ├── painting/
│   ├── sculpture/
│   ├── engraving/
├── upload_pdf_v2.html         # PDF upload interface
├── train_models_v2.html       # Training interface
```

---

## **Deployment Environment**

- **Host OS**: Windows 11
- **Virtualization**: Ubuntu running via WSL
- **Dependencies**:
  - Node.js (Express, Multer)
  - Python 3.x (transformers, datasets, torch)

### Run the server:

```bash
# Install Node.js dependencies
npm install

# Start Node.js server
node mai-base-server_v2.js
```

### Run Python scripts for dataset optimization and training:

```bash
# Optimize dataset
python3 convertData.py <model_name>

# Train model
python3 trainModel.py <model_name>
``` 

This system provides a streamlined pipeline for creating and training a state-of-the-art multimodal AI model.

# Merging and deploying model

- Merging and Running the Trained Model on Runpod
In the development stage, we merge the base LLaMA 3.2 11B Vision-Instruct model with our trained LoRA adapter for deployment. The merged model is hosted on Runpod and communicates with a website using FastAPI.

Merging Base Model and LoRA Adapter
Below is an example of how a LoRA adapter is loaded into the base model for inference:

import torch
from transformers import AutoModelForCausalLM, AutoProcessor
from peft import PeftModel, PeftConfig

# Define base model and adapter paths
base_model_id = "meta-llama/Llama-3.2-11B-Vision-Instruct"
lora_adapter_path = "loraMuseAI_v1"

# Set device
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
print(f"Selected device: {device}")

# Load base model
print("Loading base model...")
model = AutoModelForCausalLM.from_pretrained(
    base_model_id,
    torch_dtype=torch.bfloat16,
    device_map="auto"
)

# Load LoRA adapter into the model
print("Loading LoRA adapter...")
config = PeftConfig.from_pretrained(lora_adapter_path)
config.peft_type = "LORA"  # Ensure the correct type is set

model = PeftModel.from_pretrained(model, lora_adapter_path, config=config)
model.eval()

print(f"Model is running on: {next(model.parameters()).device}")

# Load processor
processor = AutoProcessor.from_pretrained(base_model_id)
Deployment Notes
Hosting Environment:

The model is deployed on Runpod, which provides an efficient cloud environment optimized for AI workloads.
Runpod is running on 1 A100 PCIe GPU (24 vCPU 125 GB RAM)
API Integration:

A FastAPI backend is used to expose endpoints for communication with the website.
This ensures seamless integration of the model's capabilities into a web application.
Usage:

The merged model is used for inference, leveraging the fine-tuned LoRA adapter to generate responses or process multimodal inputs efficiently.
Workflow Summary
Merging:

The base model (meta-llama/Llama-3.2-11B-Vision-Instruct) is augmented with the LoRA adapter (loraMuseAI_v1).
The PeftModel class integrates the adapter with the base model to retain fine-tuning benefits.
Running on Runpod:

After merging, the model runs on 1 GPU in the Runpod environment for optimized performance.
API Integration:

FastAPI handles communication between the model and the web application for real-time interaction.
This setup allows for a robust development stage with efficient model deployment and interaction.

Test it yourself:
MuseAI Live Demo at https://www.sulest.co/museai -will run from Nov. 25 to Nov. 30, 2024.
