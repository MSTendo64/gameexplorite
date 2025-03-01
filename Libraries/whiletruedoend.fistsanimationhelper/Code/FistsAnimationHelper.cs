using Sandbox;

[Title( "Fists Animation Helper" )]
public sealed class FistsAnimationHelper : Component
{
	[Property]
	SkinnedModelRenderer Target { get; set; }

	public bool IsGrounded
	{
		get => Target.GetBool( "b_grounded" );
		set => Target.Set( "b_grounded", value );
	}

	public bool IsRunning
	{
		get => Target.GetBool( "b_sprint" );
		set => Target.Set( "b_sprint", value );
	}

	public float ViewBob
	{
		get => Target.GetFloat( "move_bob" );
		set => Target.Set( "move_bob", System.Math.Clamp( value, 0f, 1f ) );
	}

	public void TriggerJump()
	{
		Target.Set( "b_jump", true );
	}

	public void TriggerAttack()
	{
		Target.Set( "b_attack", true );
	}

	public void TriggerHolster()
	{
		Target.Set( "b_holster", true );
	}

	public void TriggerDeploy()
	{
		Target.Set( "b_deploy", true );
	}
}
