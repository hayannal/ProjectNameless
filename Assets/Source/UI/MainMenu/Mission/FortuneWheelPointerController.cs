using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FortuneWheelPointerController : MonoBehaviour
{
	public AudioSource audioSource;

	public void Hithandler()
	{
		if (audioSource && !audioSource.isPlaying)
		{
			audioSource.Play();
		}
	}
}