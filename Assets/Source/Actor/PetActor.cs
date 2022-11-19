using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ECM.Controllers;

public class PetActor : MonoBehaviour
{
	public string actorId;
	public Animator animator { get; private set; }

	void Awake()
	{
		PreInitializeComponent();
	}

	void Start()
	{
		InitializeActor();
	}

	public void PreInitializeComponent()
	{
		animator = GetComponentInChildren<Animator>();
	}

	protected virtual void InitializeActor()
	{
	}

	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}