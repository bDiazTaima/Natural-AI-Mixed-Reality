
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
