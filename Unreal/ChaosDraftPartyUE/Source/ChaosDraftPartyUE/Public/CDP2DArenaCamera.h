#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "CDP2DArenaCamera.generated.h"

class UCameraComponent;

UCLASS(Blueprintable)
class CHAOSDRAFTPARTYUE_API ACDP2DArenaCamera : public AActor
{
	GENERATED_BODY()

public:
	ACDP2DArenaCamera();

	virtual void BeginPlay() override;
	virtual void Tick(float DeltaSeconds) override;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "CDP|Camera")
	TObjectPtr<UCameraComponent> Camera;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Camera")
	float CameraY = -1600.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Camera")
	float MinOrthoWidth = 1200.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Camera")
	float MaxOrthoWidth = 2600.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Camera")
	float FramingPadding = 500.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Camera")
	float VerticalBias = 160.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Camera")
	float InterpSpeed = 5.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CDP|Camera")
	bool bAutoSetViewTarget = true;

protected:
	void RefreshViewTarget();
	bool CalculateTargetFrame(FVector& OutLocation, float& OutOrthoWidth) const;

	bool bViewTargetInitialized = false;
};
