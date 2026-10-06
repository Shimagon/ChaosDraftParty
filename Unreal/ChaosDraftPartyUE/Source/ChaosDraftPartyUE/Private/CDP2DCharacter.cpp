#include "CDP2DCharacter.h"

#include "Components/InputComponent.h"
#include "Components/CapsuleComponent.h"
#include "GameFramework/CharacterMovementComponent.h"

ACDP2DCharacter::ACDP2DCharacter()
{
	PrimaryActorTick.bCanEverTick = true;

	bUseControllerRotationPitch = false;
	bUseControllerRotationYaw = false;
	bUseControllerRotationRoll = false;

	GetCapsuleComponent()->InitCapsuleSize(42.0f, 96.0f);
	SpawnCollisionHandlingMethod = ESpawnActorCollisionHandlingMethod::AdjustIfPossibleButAlwaysSpawn;

	ApplyMovementDefaults();
}

void ACDP2DCharacter::BeginPlay()
{
	Super::BeginPlay();
	ApplyMovementDefaults();
	if (USkeletalMeshComponent* CharacterMesh = GetMesh())
	{
		CharacterMesh->SetRelativeLocation(MeshRelativeOffset);
		CharacterMesh->SetRelativeScale3D(FVector(MeshRelativeScale));
	}

	if (GetActorLocation().Z < MinimumStartZ)
	{
		const FVector Location = GetActorLocation();
		SetActorLocation(FVector(Location.X, Location.Y, MinimumStartZ), false, nullptr, ETeleportType::TeleportPhysics);
	}
	SnapTo2DPlane();

	UE_LOG(LogTemp, Display, TEXT("CDP2DCharacter BeginPlay: %s class=%s"), *GetName(), *GetClass()->GetPathName());
}

void ACDP2DCharacter::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);

	UCharacterMovementComponent* Movement = GetCharacterMovement();
	if (!Movement)
	{
		return;
	}

	const bool bGrounded = Movement->IsMovingOnGround();
	TimeSinceGrounded = bGrounded ? 0.0f : TimeSinceGrounded + DeltaSeconds;

	JumpBufferRemaining = FMath::Max(0.0f, JumpBufferRemaining - DeltaSeconds);
	KeyboardMovePulseRemaining = FMath::Max(0.0f, KeyboardMovePulseRemaining - DeltaSeconds);
	DashCooldownRemaining = FMath::Max(0.0f, DashCooldownRemaining - DeltaSeconds);

	if (FMath::Abs(KeyboardMoveAxis) > KINDA_SMALL_NUMBER && KeyboardMovePulseRemaining > 0.0f)
	{
		Move2D(KeyboardMoveAxis);
	}

	if (DashTimeRemaining > 0.0f)
	{
		DashTimeRemaining = FMath::Max(0.0f, DashTimeRemaining - DeltaSeconds);
		FVector Velocity = Movement->Velocity;
		Velocity.X = DashDirection * DashSpeed;
		Velocity.Y = 0.0f;
		Movement->Velocity = Velocity;
	}

	if (Movement->Velocity.Z < -MaxFallSpeed)
	{
		FVector Velocity = Movement->Velocity;
		Velocity.Z = -MaxFallSpeed;
		Movement->Velocity = Velocity;
	}

	TryConsumeBufferedJump();
	SnapTo2DPlane();
}

void ACDP2DCharacter::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);

	if (!PlayerInputComponent)
	{
		return;
	}

	PlayerInputComponent->BindAxis(TEXT("CDP_Move2D"), this, &ACDP2DCharacter::Move2D);
	PlayerInputComponent->BindAction(TEXT("CDP_MoveLeft"), IE_Pressed, this, &ACDP2DCharacter::HandleMoveLeftPressed);
	PlayerInputComponent->BindAction(TEXT("CDP_MoveLeft"), IE_Released, this, &ACDP2DCharacter::HandleMoveLeftReleased);
	PlayerInputComponent->BindAction(TEXT("CDP_MoveRight"), IE_Pressed, this, &ACDP2DCharacter::HandleMoveRightPressed);
	PlayerInputComponent->BindAction(TEXT("CDP_MoveRight"), IE_Released, this, &ACDP2DCharacter::HandleMoveRightReleased);
	PlayerInputComponent->BindAction(TEXT("CDP_Jump"), IE_Pressed, this, &ACDP2DCharacter::RequestBufferedJump);
	PlayerInputComponent->BindAction(TEXT("CDP_Jump"), IE_Released, this, &ACDP2DCharacter::ReleaseBufferedJump);
	PlayerInputComponent->BindAction(TEXT("CDP_Dash"), IE_Pressed, this, &ACDP2DCharacter::HandleDashPressed);

	UE_LOG(LogTemp, Display, TEXT("CDP2DCharacter input bound: %s component=%s"), *GetName(), *PlayerInputComponent->GetClass()->GetName());
}

void ACDP2DCharacter::Move2D(float AxisValue)
{
	const float ClampedAxis = FMath::Clamp(AxisValue, -1.0f, 1.0f);
	if (FMath::Abs(ClampedAxis) <= KINDA_SMALL_NUMBER)
	{
		return;
	}

	LastFacingSign = ClampedAxis > 0.0f ? 1.0f : -1.0f;
	if (!FMath::IsNearlyEqual(LastLoggedMoveSign, LastFacingSign))
	{
		LastLoggedMoveSign = LastFacingSign;
		UE_LOG(LogTemp, Display, TEXT("CDP2DCharacter Move2D input: %s axis=%.2f"), *GetName(), ClampedAxis);
	}

	AddMovementInput(FVector::XAxisVector, ClampedAxis);
}

void ACDP2DCharacter::Jump()
{
	RequestBufferedJump();
}

void ACDP2DCharacter::StopJumping()
{
	ReleaseBufferedJump();
}

void ACDP2DCharacter::RequestBufferedJump()
{
	if (LastJumpRequestFrame == GFrameCounter)
	{
		return;
	}

	LastJumpRequestFrame = GFrameCounter;

	UE_LOG(LogTemp, Display, TEXT("CDP2DCharacter Jump pressed: %s"), *GetName());
	JumpBufferRemaining = JumpBufferTime;
	TryConsumeBufferedJump();
}

void ACDP2DCharacter::ReleaseBufferedJump()
{
	UCharacterMovementComponent* Movement = GetCharacterMovement();
	if (Movement && Movement->Velocity.Z > 0.0f)
	{
		FVector Velocity = Movement->Velocity;
		Velocity.Z *= JumpCutVelocityFactor;
		Movement->Velocity = Velocity;
	}

	Super::StopJumping();
}

bool ACDP2DCharacter::RequestDash()
{
	if (DashCooldownRemaining > 0.0f || DashTimeRemaining > 0.0f)
	{
		return false;
	}

	DashDirection = LastFacingSign >= 0.0f ? 1.0f : -1.0f;
	DashTimeRemaining = DashDuration;
	DashCooldownRemaining = DashCooldown;

	UE_LOG(LogTemp, Display, TEXT("CDP2DCharacter Dash pressed: %s direction=%.1f"), *GetName(), DashDirection);
	LaunchCharacter(FVector(DashDirection * DashSpeed, 0.0f, 0.0f), true, false);
	return true;
}

void ACDP2DCharacter::HandleDashPressed()
{
	RequestDash();
}

void ACDP2DCharacter::HandleMoveLeftPressed()
{
	SetKeyboardMoveAxis(-1.0f);
	UE_LOG(LogTemp, Display, TEXT("CDP2DCharacter MoveLeft pressed: %s"), *GetName());
}

void ACDP2DCharacter::HandleMoveLeftReleased()
{
	if (KeyboardMoveAxis < 0.0f)
	{
		KeyboardMoveAxis = 0.0f;
	}
}

void ACDP2DCharacter::HandleMoveRightPressed()
{
	SetKeyboardMoveAxis(1.0f);
	UE_LOG(LogTemp, Display, TEXT("CDP2DCharacter MoveRight pressed: %s"), *GetName());
}

void ACDP2DCharacter::HandleMoveRightReleased()
{
	if (KeyboardMoveAxis > 0.0f)
	{
		KeyboardMoveAxis = 0.0f;
	}
}

void ACDP2DCharacter::SetKeyboardMoveAxis(float AxisValue)
{
	KeyboardMoveAxis = FMath::Clamp(AxisValue, -1.0f, 1.0f);
	KeyboardMovePulseRemaining = 0.20f;
	Move2D(KeyboardMoveAxis);
}

void ACDP2DCharacter::ApplyKnockback(FVector KnockbackVelocity)
{
	KnockbackVelocity.Y = 0.0f;
	LaunchCharacter(KnockbackVelocity, true, true);
}

void ACDP2DCharacter::ApplyMovementDefaults()
{
	UCharacterMovementComponent* Movement = GetCharacterMovement();
	if (!Movement)
	{
		return;
	}

	Movement->MaxWalkSpeed = RunSpeed;
	Movement->MinAnalogWalkSpeed = WalkSpeed;
	Movement->MaxAcceleration = HorizontalAcceleration;
	Movement->BrakingDecelerationWalking = GroundDeceleration;
	Movement->BrakingDecelerationFalling = AirDeceleration;
	Movement->JumpZVelocity = JumpVelocity;
	Movement->AirControl = AirControlAmount;
	Movement->GravityScale = 1.0f;
	Movement->RotationRate = FRotator(0.0f, 720.0f, 0.0f);
	Movement->bOrientRotationToMovement = true;
	Movement->bConstrainToPlane = true;
	Movement->SetPlaneConstraintNormal(FVector::YAxisVector);
	Movement->SetPlaneConstraintOrigin(FVector(0.0f, PlaneY, 0.0f));
	Movement->bSnapToPlaneAtStart = true;
}

bool ACDP2DCharacter::TryConsumeBufferedJump()
{
	if (JumpBufferRemaining <= 0.0f)
	{
		return false;
	}

	UCharacterMovementComponent* Movement = GetCharacterMovement();
	if (!Movement)
	{
		return false;
	}

	if (Movement->IsMovingOnGround())
	{
		JumpBufferRemaining = 0.0f;
		Super::Jump();
		return true;
	}

	if (TimeSinceGrounded <= CoyoteTime)
	{
		JumpBufferRemaining = 0.0f;
		LaunchCharacter(FVector(0.0f, 0.0f, JumpVelocity), false, true);
		return true;
	}

	return false;
}

void ACDP2DCharacter::SnapTo2DPlane()
{
	const FVector Location = GetActorLocation();
	if (FMath::Abs(Location.Y - PlaneY) <= PlaneSnapTolerance)
	{
		return;
	}

	SetActorLocation(FVector(Location.X, PlaneY, Location.Z), false, nullptr, ETeleportType::TeleportPhysics);

	UCharacterMovementComponent* Movement = GetCharacterMovement();
	if (Movement)
	{
		FVector Velocity = Movement->Velocity;
		Velocity.Y = 0.0f;
		Movement->Velocity = Velocity;
	}
}
