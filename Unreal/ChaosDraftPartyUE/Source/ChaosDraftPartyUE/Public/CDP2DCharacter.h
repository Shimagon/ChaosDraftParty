#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "CDP2DCharacter.generated.h"

UCLASS(Blueprintable)
class CHAOSDRAFTPARTYUE_API ACDP2DCharacter : public ACharacter
{
	GENERATED_BODY()

public:
	ACDP2DCharacter();

	virtual void Tick(float DeltaSeconds) override;
	virtual void BeginPlay() override;
	virtual void SetupPlayerInputComponent(class UInputComponent* PlayerInputComponent) override;
	virtual void Jump() override;
	virtual void StopJumping() override;

	UFUNCTION(BlueprintCallable, Category = "CDP|Input")
	void Move2D(float AxisValue);

	UFUNCTION(BlueprintCallable, Category = "CDP|Input")
	void RequestBufferedJump();

	UFUNCTION(BlueprintCallable, Category = "CDP|Input")
	void ReleaseBufferedJump();

	UFUNCTION(BlueprintCallable, Category = "CDP|Input")
	bool RequestDash();

	UFUNCTION(BlueprintCallable, Category = "CDP|Combat")
	void ApplyKnockback(FVector KnockbackVelocity);

	UFUNCTION(BlueprintPure, Category = "CDP|State")
	bool IsDashActive() const { return DashTimeRemaining > 0.0f; }

	UFUNCTION(BlueprintPure, Category = "CDP|State")
	float GetLastFacingSign() const { return LastFacingSign; }

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Movement")
	float RunSpeed = 800.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Movement")
	float WalkSpeed = 150.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Movement")
	float HorizontalAcceleration = 2500.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Movement")
	float GroundDeceleration = 3600.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Movement")
	float AirDeceleration = 900.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Movement")
	float AirControlAmount = 0.65f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Jump")
	float JumpVelocity = 500.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Jump")
	float CoyoteTime = 0.08f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Jump")
	float JumpBufferTime = 0.10f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Jump", meta = (ClampMin = "0.0", ClampMax = "1.0"))
	float JumpCutVelocityFactor = 0.55f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Jump")
	float MaxFallSpeed = 1800.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Dash")
	float DashSpeed = 1200.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Dash")
	float DashDuration = 0.16f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Dash")
	float DashCooldown = 0.35f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Plane")
	float PlaneY = 0.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Plane")
	float PlaneSnapTolerance = 0.5f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Spawn")
	float MinimumStartZ = 160.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Visual")
	FVector MeshRelativeOffset = FVector(0.0f, 0.0f, -80.0f);

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Visual")
	float MeshRelativeScale = 0.5f;

protected:
	void ApplyMovementDefaults();
	bool TryConsumeBufferedJump();
	void SnapTo2DPlane();
	void HandleDashPressed();
	void HandleMoveLeftPressed();
	void HandleMoveLeftReleased();
	void HandleMoveRightPressed();
	void HandleMoveRightReleased();
	void SetKeyboardMoveAxis(float AxisValue);

	float TimeSinceGrounded = 0.0f;
	float JumpBufferRemaining = 0.0f;
	float DashTimeRemaining = 0.0f;
	float DashCooldownRemaining = 0.0f;
	float DashDirection = 1.0f;
	float LastFacingSign = 1.0f;
	float LastLoggedMoveSign = 0.0f;
	float KeyboardMoveAxis = 0.0f;
	float KeyboardMovePulseRemaining = 0.0f;
	uint64 LastJumpRequestFrame = 0;
};
