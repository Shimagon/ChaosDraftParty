# **3Dキャラクター仕様書：ガチャガチャ・カプセルトイ型キャラクター**

## **1\. 基本コンセプト**

* **テーマ**: ガチャガチャのカプセルをモチーフにした、変形ギミックを持つシンプルなマスコットキャラクター。  
* **カラーと材質**:  
  * 基本はソリッドなピンク色と、透明（クリア）素材の組み合わせ。  
  * 光沢のあるツルツルとした硬質プラスチック（射出成形されたカプセルトイのような質感）。表面は完全に滑らかで、毛や布などのテクスチャは一切ない。

## **2\. 形態と変形ギミック（モジュラー構造）**

このキャラクターは2つの状態を持つ。

* **形態A：通常時（フルフォーム）**  
  * 2つの球体が上下に重なった、雪だるまのようなシルエット。  
  * 上の球体（頭部）が下の球体（胴体）に乗っている状態。くっついてない。乗っかってる。  
* **形態B：しゃがみ・収納時（コンパクトフォーム）**  
  * 頭部が胴体の中にスライドして沈み込み、全体が1つの大きな丸いカプセル状に収納された状態。  
  * この時、顔のパーツはカプセル表面に露出している、または透明なカバー越しに透けて見える。

## **3\. 各パーツの詳細**

### **頭部（形態Aで露出する部分）**

* 形状: 完全な球体。髪の毛や凹凸は一切ない、ツルツルの不透明なピンクのプラスチック球。  
* 顔面:  
  * 目: シンプルで小さな黒い円が2つ。  
  * 口: その下に、シンプルな上向きの緩やかなカーブを描く線（U字型の笑顔）。

### **胴体（カプセル下部）**

* 形状: 頭部より一回り大きい球体をベースにした形状。  
* カプセルの継ぎ目: 胴体の上部（赤道面より少し上）に、水平に一周する溝（パーティングライン/ガチャガチャの上下を分ける切れ目）がある。  
* **【重要】透明と不透明の切り替わり**: 実際のガチャガチャのカプセルのように、この水平の切れ目を境にして、**下半分は不透明なピンク**のプラスチック、\*\*上半分は中身が見える透明（クリア）\*\*なプラスチックになっている。  
* この切れ目の上の部分（透明な部分）から、形態Bの際に頭部が出し入れされる構造。形態Bでは、透明なパーツの内側に頭部が収納されている様子が透けて見える。

### **手足**

* 腕（手）: 胴体の左右の下半分（不透明ピンクの部分）に付いている、短く丸みを帯びたふっくらしたピンクのプラスチックの突起。指はない。  
* 脚（足）: 胴体の真下から生える、短く丸みを帯びた2つのピンクの突起。足首や指はなく、接地部分は少し平らになっていて自立できる。

## **4\. 3Dレンダリング時の指示**

* 背景: クリーンなニュートラルグレー、またはホワイトのスタジオ背景。影を落として接地感を出すこと。  
* 照明: プラスチック特有のツヤ（ハイライト）と、透明パーツのガラスのような質感が強調される明るいスタジオライティング。

![][image1]English Prompt for 3D Generation AI

(海外の3D生成AIツールを使用する場合の英訳プロンプト)

**Subject:** A simple, cute capsule-toy (Gachapon) mascot character made of glossy, smooth plastic.

**Features & Forms:**

The character has a modular, transformable design with two states:

1. **Standing Form:** Looks like a snowman made of two spheres. A perfectly round, solid pink head resting on a slightly larger spherical body.  
2. **Retracted/Squatting Form:** The head retracts down into the body, forming a single, compact capsule shape.

**Details:**

* **Material & Color:** Highly reflective, shiny hard plastic (like a real vending machine capsule).  
  * The head, arms, and legs are solid, opaque pink plastic.  
  * **The body is half clear, half opaque:** There is a distinct horizontal groove (parting line) wrapping around the upper chest area of the body. The bottom half below the line is opaque pink, and the top half above the line is completely transparent/clear plastic.  
* **Face:** Very simple. Two small black dot eyes and a simple curved line for a smiling mouth on the head. In the retracted form, the head is visible through the transparent top half of the body capsule.  
* **Limbs:** Short, stubby, rounded, chubby opaque pink plastic arms on the sides and two short, rounded nub-like legs at the bottom to stand on. No fingers or toes.

**Environment:** Clean white studio background, bright studio lighting to show off the glossy plastic specular highlights and the transparency of the clear plastic parts.

[image1]: <data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAmwAAAA9CAYAAAAQ2DVeAAAD+0lEQVR4Xu3c3aulUxgA8DkmhGIu1OHYe5+9zznCiaSTyLhhZPIVU2585GJ8ZHItKZELH6W4oiZz4WqIcCUpf4DGzSRyr2Ok3MgUhoznOa2VZc2ejtLUPjO/X63Wep5nrfd9z93Tu/c+27YBAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJuY6xMAAMyQxcXF431uVsWz/pTPm3NfAwA4LY1Goze2UsM2Pz9/QWnYnq651dXVc7bS3wAA8J9Fk7MnxutbqdkZj8f7ttLzAgD8L7XxyXllZeXCvj6L4ll/1LABAGeMwWBwTc7ZAI1Go51tbTwe747cbSXcHns+XlpaGnX1V2o91u+09SpyF8XZ12L/i20+ch9E7oZcx9nViA9MO5+ivhT1d2P/vfmsMT5varsifrPdX0X+YL1mnH014uf6PQAAM6t9S1UatudrHOuXS6OVzVG+fTu37DsWte+a+t5p9fa6MW1v4u/LvF7rMf5s6vkcj9S45H6r64WFhYu7+22cjfnryWRyRXPmcDRoO+qePLO2tnZ2ud/Bug8AYGZFM7O/vt1K2chE/H4bN/kHmvyXpenZqEft0LR6PVvXKZut2HtjqR2N9SVlz03N+bzfvib+pL1G3dOs7+hzJT7crPMeR8oPE47Gc5zf7gUAmEVzpYnJt1CHFv9pwr7tN05phHLfv97MTavHuLTMv8Q4kuu2ISx735p2Pqaz2jjGDzUeDAaXR/xrjdNkMrm2v06rXOPRPg8AMLOmNTeR+6jP13+f0eZK8/PhZvX8eLWv9creL2o8HA6vjvjnGsc1nsg93Ued6zFuqXHJZTP4VZurIv/sZs8BADBzojFa7nPR1LzUNzb5PbXIHWj2vN3uKfUTGrZSu7mvlfzDdV0ard1N/E00Z7eX9e+x9/7+Gs31H88fItTvtJWPVz+N0lysd2Qu/868TnuNvEddAwDMnGiGFvsGqIrm54W+FvEfi80br9IYXdnV22Zovasfj7G3xtnEdQ1af7/ajD0TY7XmlpeXh2Wdb9c29sR8rMyPNec+yznusT9zg8HgvPIMf2U+PzqNPe/lGgDgtNA0QjuHw+H10+oxHjxZPZUv+m/8MKB3kjd9d/a5aLTm8xepNY773d3Wy5u0W9tcNGuXReN2X42j/lBbBwDY8qLBuas2bNNsVgcA4BSKRuyp8vYsxwm/rozcVbU+Ho+f7OsAAJxio9Honhi78vtm4/J/01rRqF1X67He09cBAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADgzPM39mgcRgjGjqkAAAAASUVORK5CYII=>