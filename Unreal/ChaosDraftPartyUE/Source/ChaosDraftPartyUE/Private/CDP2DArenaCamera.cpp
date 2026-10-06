#include "CDP2DArenaCamera.h"

#include "CDP2DCharacter.h"
#include "Camera/CameraComponent.h"
#include "EngineUtils.h"
#include "GameFramework/PlayerController.h"
#include "Kismet/GameplayStatics.h"

ACDP2DArenaCamera::ACDP2DArenaCamera()
{
	PrimaryActorTick.bCanEverTick = true;

	Camera = CreateDefaultSubobject<UCameraComponent>(TEXT("ArenaCamera"));
	SetRootComponent(Camera);

	Camera->ProjectionMode = ECameraProjectionMode::Orthographic;
	Camera->OrthoWidth = MinOrthoWidth;
	SetActorRotation(FRotator(0.0f, 90.0f, 0.0f));
}

void ACDP2DArenaCamera::BeginPlay()
{
	Super::BeginPlay();
	if (!bViewTargetInitialized)
	{
		RefreshViewTarget();
	}
}

void ACDP2DArenaCamera::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);

	FVector TargetLocation = GetActorLocation();
	float TargetOrthoWidth = Camera ? Camera->OrthoWidth : MinOrthoWidth;
	if (!CalculateTargetFrame(TargetLocation, TargetOrthoWidth))
	{
		return;
	}

	const float EffectiveInterpSpeed = FMath::Max(0.0f, InterpSpeed);
	const FVector NewLocation = EffectiveInterpSpeed > 0.0f
		? FMath::VInterpTo(GetActorLocation(), TargetLocation, DeltaSeconds, EffectiveInterpSpeed)
		: TargetLocation;
	SetActorLocation(NewLocation);

	if (Camera)
	{
		Camera->SetOrthoWidth(EffectiveInterpSpeed > 0.0f
			? FMath::FInterpTo(Camera->OrthoWidth, TargetOrthoWidth, DeltaSeconds, EffectiveInterpSpeed)
			: TargetOrthoWidth);
	}

	RefreshViewTarget();
}

void ACDP2DArenaCamera::RefreshViewTarget()
{
	if (!bAutoSetViewTarget)
	{
		return;
	}

	if (APlayerController* PlayerController = UGameplayStatics::GetPlayerController(this, 0))
	{
		PlayerController->bAutoManageActiveCameraTarget = false;
		if (PlayerController->GetViewTarget() != this)
		{
			PlayerController->SetViewTarget(this);
		}
		bViewTargetInitialized = true;
	}
}

bool ACDP2DArenaCamera::CalculateTargetFrame(FVector& OutLocation, float& OutOrthoWidth) const
{
	const UWorld* World = GetWorld();
	if (!World)
	{
		return false;
	}

	bool bFoundAnyCharacter = false;
	float MinX = TNumericLimits<float>::Max();
	float MaxX = TNumericLimits<float>::Lowest();
	float MinZ = TNumericLimits<float>::Max();
	float MaxZ = TNumericLimits<float>::Lowest();

	for (TActorIterator<ACDP2DCharacter> It(World); It; ++It)
	{
		const ACDP2DCharacter* Character = *It;
		if (!IsValid(Character))
		{
			continue;
		}

		const FVector Location = Character->GetActorLocation();
		MinX = FMath::Min(MinX, Location.X);
		MaxX = FMath::Max(MaxX, Location.X);
		MinZ = FMath::Min(MinZ, Location.Z);
		MaxZ = FMath::Max(MaxZ, Location.Z);
		bFoundAnyCharacter = true;
	}

	if (!bFoundAnyCharacter)
	{
		return false;
	}

	const float CenterX = (MinX + MaxX) * 0.5f;
	const float CenterZ = ((MinZ + MaxZ) * 0.5f) + VerticalBias;
	const float NeededWidth = (MaxX - MinX) + FramingPadding;
	const float NeededHeightAsWidth = ((MaxZ - MinZ) + FramingPadding) * 16.0f / 9.0f;

	OutLocation = FVector(CenterX, CameraY, CenterZ);
	OutOrthoWidth = FMath::Clamp(FMath::Max3(MinOrthoWidth, NeededWidth, NeededHeightAsWidth), MinOrthoWidth, MaxOrthoWidth);
	return true;
}
