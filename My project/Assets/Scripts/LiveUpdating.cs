using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class LiveUpdating : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_InputField _fileLocation;

    FMOD.Studio.EVENT_CALLBACK _playFileCallBack;

    [SerializeField] FMODUnity.EventReference _eventRef;

    FMOD.Studio.EventInstance _instance;

    // Start is called before the first frame update
    void Start()
    {
        _playFileCallBack = new FMOD.Studio.EVENT_CALLBACK(PlayFileCallBack);
        _instance = FMODUnity.RuntimeManager.CreateInstance(_eventRef);
    }

    // Update is called once per frame
    void Update()
    {
        //Start the music from file 
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_fileLocation.text.Length != 0)
            {
                UnityEngine.Debug.Log(_fileLocation.text);
                PlayAudioFile(_fileLocation.text);
            }
            else
            {
                UnityEngine.Debug.Log("Missing File Location: " + _fileLocation.text);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            _instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
    }

    public void PlayAudioFile(string location)
    {
        FMOD.RESULT result;
        if (_instance.isValid() == false)
        {
            UnityEngine.Debug.Log("Failed to create instance when playing sound");
            return;
        }
        // Pin the location in memory and pass a pointer through to user data 
        GCHandle stringHandle = GCHandle.Alloc(location);
        _instance.setUserData(GCHandle.ToIntPtr(stringHandle));

        result = _instance.setCallback(_playFileCallBack);
        result = _instance.start();
        //result = _instance.release();

        UnityEngine.Debug.Log("Played Sound");
    }


    [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
    static FMOD.RESULT PlayFileCallBack(FMOD.Studio.EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        FMOD.Studio.EventInstance instance = new FMOD.Studio.EventInstance(instancePtr);

        if (instance.isValid() == false)
        {
            UnityEngine.Debug.Log("Failed to create instance in call back");
            return FMOD.RESULT.ERR_EVENT_NOTFOUND;
        }

        //Retrive the user data 
        IntPtr stringPtr;
        instance.getUserData(out stringPtr);
        //Get the string object
        GCHandle stringHandle = GCHandle.FromIntPtr(stringPtr);
        String location = stringHandle.Target as String;

        switch (type)
        {
            case FMOD.Studio.EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND:
                {
                    FMOD.MODE soundMode = FMOD.MODE.CREATESTREAM;
                    var parameter = (FMOD.Studio.PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.PROGRAMMER_SOUND_PROPERTIES));

                    FMOD.Sound audioSound;
                    FMOD.RESULT result;

                    result = FMODUnity.RuntimeManager.CoreSystem.createSound(location, soundMode, out audioSound);

                    if (result == FMOD.RESULT.OK)
                    {
                        parameter.sound = audioSound.handle;
                        parameter.subsoundIndex = -1;
                        Marshal.StructureToPtr(parameter, parameterPtr, false);
                    }
                    break;
                }
            case FMOD.Studio.EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND:
                {
                    var paramerter = (FMOD.Studio.PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.PROGRAMMER_SOUND_PROPERTIES));
                    var sound = new FMOD.Sound(paramerter.sound);
                    sound.release();
                    Debug.Log("Released the programmer sound sound");
                    break;
                }
            case FMOD.Studio.EVENT_CALLBACK_TYPE.DESTROYED:
                {
                    // Now the event has been destroyed, unpin the string memory so it can be garbaged collected
                    stringHandle.Free();
                    break;
                }
        }

        return FMOD.RESULT.OK;
    }


}
