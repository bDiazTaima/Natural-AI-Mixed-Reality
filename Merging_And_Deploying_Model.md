
# Merging and Running the Trained Model on Runpod

In the development stage, we merge the **base LLaMA 3.2 11B Vision-Instruct model** with our trained **LoRA adapter** for deployment. The merged model is hosted on **Runpod** and communicates with a website using **FastAPI**.

---

## **Merging Base Model and LoRA Adapter**

Below is an example of how a LoRA adapter is loaded into the base model for inference:

```python
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
```

---

## **Deployment Notes**

1. **Hosting Environment**:
   - The model is deployed on **Runpod**, which provides an efficient cloud environment optimized for AI workloads.
   - Runpod is running on 1 A100 PCIe GPU (24 vCPU 125 GB RAM)
   
2. **API Integration**:
   - A **FastAPI** backend is used to expose endpoints for communication with the website.
   - This ensures seamless integration of the model's capabilities into a web application.

3. **Usage**:
   - The merged model is used for inference, leveraging the fine-tuned LoRA adapter to generate responses or process multimodal inputs efficiently.

---

## **Workflow Summary**

- **Merging**:
  - The base model (`meta-llama/Llama-3.2-11B-Vision-Instruct`) is augmented with the LoRA adapter (`loraMuseAI_v1`).
  - The `PeftModel` class integrates the adapter with the base model to retain fine-tuning benefits.

- **Running on Runpod**:
  - After merging, the model runs on 1 GPU in the Runpod environment for optimized performance.

- **API Integration**:
  - **FastAPI** handles communication between the model and the web application for real-time interaction.

This setup allows for a robust development stage with efficient model deployment and interaction.

- **Test it yourself**:
  - **MuseAI Live Demo** at [https://www.sulest.co/museai](https://www.sulest.co/museai) -will run from Nov. 25 to Nov. 30, 2024.

