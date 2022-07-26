using FMODUnity;
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices; 
public class RecordMic : MonoBehaviour
{
	//public variables
	[Header("Choose A Microphone")]
	public int RecordingDeviceIndex = 0;
	[TextArea] public string RecordingDeviceName = null;
	[Header("How Long In Seconds Before Recording Plays")]
	public float Latency = 1f;
	[Header("Choose A Key To Play/Pause/Add Reverb To Recording")]
	public KeyCode PlayAndPause;
	public KeyCode ReverbOnOffSwitch;

	//FMOD Objects
	private FMOD.Sound sound;
	private FMOD.CREATESOUNDEXINFO exinfo;
	private FMOD.Channel channel;
	private FMOD.ChannelGroup channelGroup;

	//How many recording devices are plugged in for us to use.
	private int numOfDriversConnected = 0;
	private int numofDrivers = 0;

	//Info about the device we're recording with.
	private System.Guid MicGUID;
	private int SampleRate = 0;
	private FMOD.SPEAKERMODE FMODSpeakerMode;
	private int NumOfChannels = 0;
	private FMOD.DRIVER_STATE driverState;

	//Other variables.
	private bool dspEnabled = false;
	private bool playOrPause = true;
	private bool playOkay = false;

	private FMOD.System micRecordSystem;
	private string extraDriverData = "MICTEST";

	private bool killed = false; 
	void Start()
	{
		GCHandle test = GCHandle.Alloc(extraDriverData); 

		FMOD.RESULT result;
		//Creating the mic system solely for recording mic input 
		result = FMOD.Factory.System_Create(out micRecordSystem);
		Debug.Log("Creating Mic System result " + result);


		

		result = micRecordSystem.init(100, FMOD.INITFLAGS.NORMAL, GCHandle.ToIntPtr(test));
		
		if (result != FMOD.RESULT.OK)
		{
			Debug.Log("System init failed with result " + result);
			micRecordSystem.release();
			return;
		}
		else
			Debug.Log("Initialized system with result " + result);


		//Step 1: Check to see if any recording devices (or drivers) are plugged in and available for us to use.

		micRecordSystem.getRecordNumDrivers(out numofDrivers, out numOfDriversConnected);

		if (numOfDriversConnected == 0)
		{
			Debug.Log("Hey! Plug a Microhpone in ya dummy!!!");
			micRecordSystem.release();
			return; 
		}
		else
			Debug.Log("You have " + numOfDriversConnected + " microphones available to record with.");


		//Step 2: Get all of the information we can about the recording device (or driver) that we're
		//        going to use to record with.


		micRecordSystem.getRecordDriverInfo(RecordingDeviceIndex, out RecordingDeviceName, 50,
			out MicGUID, out SampleRate, out FMODSpeakerMode, out NumOfChannels, out driverState);


		//Next we want to create an "FMOD Sound Object", but to do that, we first need to use our 
		//FMOD.CREATESOUNDEXINFO variable to hold and pass information such as the sample rate we're
		//recording at and the num of channels we're recording with into our Sound object.


		//Step 3: Store relevant information into FMOD.CREATESOUNDEXINFO variable.


		exinfo.cbsize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
		exinfo.numchannels = NumOfChannels;
		exinfo.format = FMOD.SOUND_FORMAT.PCM16;
		exinfo.defaultfrequency = SampleRate;
		exinfo.length = (uint)SampleRate * sizeof(short) * (uint)NumOfChannels * 100;


		//Step 4: Create an FMOD Sound "object". This is what will hold our voice as it is recorded.

		result = micRecordSystem.createSound(exinfo.userdata, FMOD.MODE.LOOP_NORMAL | FMOD.MODE.OPENUSER,
			ref exinfo, out sound);
		//micRecordSystem.createSound("Name", FMOD.MODE.LOOP_NORMAL, out FMOD.Sound newSound);
		//micRecordSystem.playSound(newSound, channelGroup, false, out FMOD.Channel newChannel);
		//sound.release(); 
		//micRecordSystem.getOutput(out FMOD.OUTPUTTYPE type);
		//Debug.Log("current output type: " + type);
	}


	void Update()
	{
		if (!killed)
			micRecordSystem.update();

		if (Input.GetKeyDown(PlayAndPause) && playOkay)
		{
			playOrPause = !playOrPause;
			channel.setPaused(playOrPause);
			Debug.Log(playOrPause);
		}

		if (Input.GetKeyDown(KeyCode.R))
		{
			FMOD.RESULT result;
			result = micRecordSystem.recordStart(RecordingDeviceIndex, sound, true);
			if (result != FMOD.RESULT.OK)
			{
				Debug.Log("record start failed with result " + result);
				micRecordSystem.release();
				return; 
			}
			else
				Debug.Log("Recording Started with result " + result);

			StartCoroutine(Wait());
			//micRecordSystem.getMasterChannelGroup(out FMOD.ChannelGroup master);
			result = micRecordSystem.playSound(sound, channelGroup, false, out channel);
			if (result != FMOD.RESULT.OK)
			{
				Debug.Log("Play sound in coroutine failed with result " + result);
				micRecordSystem.release();
			}
			else
				Debug.Log("Recording Started with result " + result);

            //channel.setPaused(false); 

            result = micRecordSystem.setOutput(FMOD.OUTPUTTYPE.WAVWRITER);
            if (result != FMOD.RESULT.OK)
            {
                Debug.Log("change output in coroutine failed with result " + result);
                micRecordSystem.release();
            }
            else
                Debug.Log("Changing output with result of " + result);
        }

		IEnumerator Wait()
        {
			yield return new WaitForSeconds(Latency);
			Debug.Log("Finished waiting"); 
        }

		if (Input.GetKeyDown(KeyCode.S))
		{
			//Stopping the recording
			FMOD.RESULT result = micRecordSystem.recordStop(RecordingDeviceIndex);
			if (result != FMOD.RESULT.OK)
			{
				Debug.Log("record stop failed with result " + result);
				micRecordSystem.release();
				return;
			}
			else
				Debug.Log("Recording Stopped with result " + result);

			StartCoroutine(Wait());
			//////Kill all processed so the file can be read in by the programmer sound  
			//result = micRecordSystem.setOutput(FMOD.OUTPUTTYPE.WASAPI);
			//if (result != FMOD.RESULT.OK)
			//{
			//	Debug.Log("change output failed with result " + result);
			//	micRecordSystem.release();
			//	return;
			//}
			//else
			//	Debug.Log("Changing output with result of " + result);
			result = sound.release();
			if (result != FMOD.RESULT.OK)
			{
				Debug.Log("sound release failed with result " + result);
				micRecordSystem.release();
				return;
			}
			else
				Debug.Log("Released sound with result " + result);
			micRecordSystem.release();
			//Instead of killing the system change the output
			Debug.Log("Changed output and killed sound");
		}

		if (Input.GetKeyDown(KeyCode.L))
		{
			////Changing the output back to the file 
			//FMOD.RESULT result = micRecordSystem.setOutput(FMOD.OUTPUTTYPE.WAVWRITER);
			//if (result != FMOD.RESULT.OK)
			//{
			//	Debug.Log("change output failed with result " + result);
			//	micRecordSystem.release();
			//	return;
			//}
			//else
			//	Debug.Log("Changing output type with result of " + result);


			FMOD.RESULT result = micRecordSystem.createSound(exinfo.userdata, FMOD.MODE.LOOP_NORMAL | FMOD.MODE.OPENUSER,
			ref exinfo, out sound);
			if (result != FMOD.RESULT.OK)
			{
				Debug.Log("create sound failed with result " + result);
				micRecordSystem.release();
				return;
			}
			else
				Debug.Log("Recreating sound to be used again with result " + result); 
		}


		
		//Optional
		//Step 8: Set a reverb to the Sound object we're recording into and turn it on or off with a new button.


		if (Input.GetKeyDown(ReverbOnOffSwitch))
		{
			FMOD.REVERB_PROPERTIES propOn = FMOD.PRESET.ROOM();
			FMOD.REVERB_PROPERTIES propOff = FMOD.PRESET.OFF();

			dspEnabled = !dspEnabled;

			RuntimeManager.CoreSystem.setReverbProperties(1, ref dspEnabled ? ref propOn : ref propOff);
		}

	}

    //    if (Input.GetKeyDown(KeyCode.R))
    //{
    //    FMOD.RESULT result;
    //    //Recording the mic input 
    //    result = micRecordSystem.recordStart(RecordingDeviceIndex, sound, true);
    //    Debug.Log("Recording Started with result " + result); 
    //}

    //if (Input.GetKeyDown(KeyCode.S))
    //{
    //    //Stopping the recording
    //    FMOD.RESULT result = micRecordSystem.recordStop(RecordingDeviceIndex);
    //    Debug.Log("Recording Stopped with result " + result);
    //}

    //if (Input.GetKeyDown(KeyCode.K))
    //{
    //    //Kill all processed so the file can be read in by the programmer sound  
    //    sound.release();
    //    micRecordSystem.release();
    //    Debug.Log("Killed everything");
    //}

    //if (Input.GetKeyDown(KeyCode.L))
    //{
    //    //Having to re-create everything the next time you want to record from the mic
    //    FMOD.RESULT result = FMOD.Factory.System_Create(out micRecordSystem);
    //    Debug.Log("Creating the whole Mic System again  result " + result);

    //    GCHandle test = GCHandle.Alloc(extraDriverData);
    //    result = micRecordSystem.init(10, FMOD.INITFLAGS.NORMAL, GCHandle.ToIntPtr(test));
    //    Debug.Log("Initialize system again with result " + result);


    //    result = micRecordSystem.createSound("Sound", FMOD.MODE.LOOP_NORMAL | FMOD.MODE.OPENUSER,
    //    ref exinfo, out sound);
    //    Debug.Log("Recreating sound to be used again with result " + result);
    //}

	

    private void OnDestroy()
    {
		micRecordSystem.release();
    }

}