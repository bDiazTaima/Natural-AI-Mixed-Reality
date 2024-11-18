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
