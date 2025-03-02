using System.Numerics;
using System;
using Microsoft.Win32;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.UI;

public sealed class PlayerCTRL : Component
{

	Logger logger = new Logger( "PLCTRL" );

	Rigidbody rb;

	int horUp, horDown, verLeft, verRight;

	[Property, Range( 1, 10 )] private float lookSpeedX = 2.0f;
	[Property, Range( 1, 10 )] private float lookSpeedY = 2.0f;
	[Property, Range( 1, 180 )] private float upperLookLimit = 80.0f;
	[Property, Range( 1, 180 )] private float lowerLookLimit = 80.0f;

	[Property] GameObject camera;

	private float rotationX = 0;

	protected override void OnStart()
	{
		rb = GetComponent<Rigidbody>();
	}

	protected override void OnUpdate()
	{
		GetInput();
		ApplyMove();

		HandMouseLook();
	}

	void GetInput()
	{
		if ( Input.Down( "Forward" ) )
		{
			horUp = 1;
		}
		else
		{
			horUp = 0;
		}

		if ( Input.Down( "Backward" ) )
		{
			horDown = -1;
		}
		else
		{
			horDown = 0;
		}

		if ( Input.Down( "Left" ) )
		{
			verRight = 1;
		}
		else
		{
			verRight = 0;
		}

		if ( Input.Down( "Right" ) )
		{
			verLeft = -1;
		}
		else
		{
			verLeft = 0;
		}
	}

	void ApplyMove()
	{
		rb.Velocity = new Vector3( (horUp + horDown) * 100, (verRight + verLeft) * 100, 0 );
	}

	private void HandMouseLook()
	{
		try
		{
			rotationX = Math.Clamp( rotationX, -upperLookLimit, lowerLookLimit );
			camera.Transform.LocalRotation = Quaternion.CreateFromYawPitchRoll( rotationX, 0, 0 );
			Transform.Rotation = Quaternion.CreateFromYawPitchRoll( 0, (Mouse.Position.x * lookSpeedX) * Transform.Rotation.y, 0 );

			logger.Info( $"Mouse X: {Mouse.Position.x} Mouse Y: {Mouse.Position.y}" );

		}catch (Exception ex )
		{
			logger.Error( ex );
		}
		
	}
}
