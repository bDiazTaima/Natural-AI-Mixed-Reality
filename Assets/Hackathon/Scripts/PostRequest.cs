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