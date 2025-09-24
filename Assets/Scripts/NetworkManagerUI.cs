using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class NewEmptyCSharpScript : MonoBehaviour
{
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;

    [SerializeField] private TMP_InputField inputIP;
    [SerializeField] private TMP_InputField inputPort;
    
    [SerializeField] private Slider slider;
    private float prevSliderValue;
    [SerializeField] private AudioMixer mixer;
    


private void Awake()
    {
        serverBtn.onClick.AddListener(() => {
            EnterIPAndPort();
            NetworkManager.Singleton.StartServer();
        });
        hostBtn.onClick.AddListener(() => {
            EnterIPAndPort();
            NetworkManager.Singleton.StartHost();
        });
        clientBtn.onClick.AddListener(() => {
            EnterIPAndPort();
            NetworkManager.Singleton.StartClient();
        });
    }

    public void EnterIPAndPort()
    {
        var inputText = inputIP.text;
        
        //ushort port = 7777;
        short tempShort;
        Int16.TryParse(inputPort.text, out tempShort);
        ushort port = (ushort)tempShort;
        
        string listenAddress = null;

        if (inputText == null || port == null)
        {
            Debug.LogError("IP or Port is null!");
            return;
        }
        
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(inputText, port, listenAddress);
    }

    private void Update()
    {
        var value = (slider.value*100-80)/2;
        
        
        if (slider.value != prevSliderValue)
            mixer.SetFloat("MasterVolume", slider.value < 0.1f ? -80f : value);
        
        prevSliderValue = slider.value;
    }
}
