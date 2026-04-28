"""
研究室発表用 pptx 生成 (2026-04-28)
- 4 月のキャラクター制作 (EGGYBOT) を一旦終わらせる総括
- 進捗 / 心境 / 良かったこと / 悪かったこと / 発見 / スクショ
"""
import os
from pptx import Presentation
from pptx.util import Inches, Pt, Emu
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN
from pptx.enum.shapes import MSO_SHAPE

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SHOTS = os.path.join(ROOT, "Assets", "Screenshots")
PROJ_PNG = os.path.join(ROOT, "Assets", "_Project")
CHAR    = os.path.join(ROOT, "キャラクター")

OUT = os.path.join(ROOT, "docs", "presentations", "2026-04-28_キャラクター総括.pptx")

# ---------- カラーパレット (Fall Guys 風) ----------
PINK   = RGBColor(0xFF, 0x6F, 0x9F)
PINK_L = RGBColor(0xFF, 0xB8, 0xD0)
GRAY_D = RGBColor(0x33, 0x33, 0x33)
GRAY_L = RGBColor(0x99, 0x99, 0x99)
ACCENT = RGBColor(0xFF, 0xC1, 0x07)
BG     = RGBColor(0xFA, 0xFA, 0xFA)

prs = Presentation()
prs.slide_width  = Inches(13.333)
prs.slide_height = Inches(7.5)
SW, SH = prs.slide_width, prs.slide_height

BLANK = prs.slide_layouts[6]

def add_bg(slide, color=BG):
    bg = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, 0, 0, SW, SH)
    bg.fill.solid()
    bg.fill.fore_color.rgb = color
    bg.line.fill.background()
    return bg

def add_text(slide, x, y, w, h, text, size=18, bold=False, color=GRAY_D, align=PP_ALIGN.LEFT, font="Hiragino Kaku Gothic ProN"):
    tb = slide.shapes.add_textbox(x, y, w, h)
    tf = tb.text_frame
    tf.word_wrap = True
    tf.margin_left = tf.margin_right = Inches(0.05)
    tf.margin_top = tf.margin_bottom = Inches(0.02)
    para = tf.paragraphs[0]
    para.alignment = align
    run = para.add_run()
    run.text = text
    run.font.name = font
    run.font.size = Pt(size)
    run.font.bold = bold
    run.font.color.rgb = color
    return tb

def add_bullets(slide, x, y, w, h, items, size=16, color=GRAY_D):
    tb = slide.shapes.add_textbox(x, y, w, h)
    tf = tb.text_frame
    tf.word_wrap = True
    for i, item in enumerate(items):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.alignment = PP_ALIGN.LEFT
        p.space_after = Pt(6)
        run = p.add_run()
        run.text = "• " + item
        run.font.name = "Hiragino Kaku Gothic ProN"
        run.font.size = Pt(size)
        run.font.color.rgb = color
    return tb

def add_band(slide, color, h_in=0.6):
    band = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, 0, 0, SW, Inches(h_in))
    band.fill.solid()
    band.fill.fore_color.rgb = color
    band.line.fill.background()
    return band

def add_image_safe(slide, path, x, y, w=None, h=None):
    if not os.path.exists(path):
        # placeholder
        ph = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, x, y, w or Inches(3), h or Inches(2))
        ph.fill.solid()
        ph.fill.fore_color.rgb = RGBColor(0xEE, 0xEE, 0xEE)
        tf = ph.text_frame
        tf.text = "(image not found)"
        return None
    pic = slide.shapes.add_picture(path, x, y, width=w, height=h)
    return pic

def add_caption(slide, x, y, w, text, size=11, color=GRAY_L):
    return add_text(slide, x, y, w, Inches(0.3), text, size=size, color=color, align=PP_ALIGN.CENTER)


# ============================================================
# Slide 1: Title
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s, PINK_L)
add_text(s, Inches(0.5), Inches(2.0), Inches(12), Inches(1.2),
         "EGGYBOT 4 月総括", size=54, bold=True, color=GRAY_D, align=PP_ALIGN.CENTER)
add_text(s, Inches(0.5), Inches(3.2), Inches(12), Inches(0.8),
         "ChaosDraftParty マスコットキャラクターの 3D 化", size=22, color=GRAY_D, align=PP_ALIGN.CENTER)
add_text(s, Inches(0.5), Inches(5.5), Inches(12), Inches(0.6),
         "2026-04-28 / 島田 / 小坂研究室", size=18, color=GRAY_D, align=PP_ALIGN.CENTER)
add_text(s, Inches(0.5), Inches(6.2), Inches(12), Inches(0.6),
         "卒業制作 M1 (4 月) — 16 イテレーション、commit 80+, 調査 1 ヶ月", size=14, color=GRAY_L, align=PP_ALIGN.CENTER)

# ============================================================
# Slide 2: 結論を先に
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s)
add_band(s, PINK)
add_text(s, Inches(0.5), Inches(0.05), Inches(12), Inches(0.5),
         "結論を先に", size=24, bold=True, color=RGBColor(0xFF, 0xFF, 0xFF))

add_text(s, Inches(0.5), Inches(1.0), Inches(12), Inches(0.7),
         "EGGYBOT v16 — 「動かしてもズレない」3D キャラができた",
         size=28, bold=True, color=PINK)

add_bullets(s, Inches(0.7), Inches(2.0), Inches(8.0), Inches(4.0), [
    "見た目: ガチャカプセル型のピンク容器、頭が出てる、手足は球",
    "胴体は Blender 製、手足は Unity の primitive Sphere",
    "Mixamo Humanoid アニメを「無加工」で流せる仕組みを構築中",
    "Skinning（スキン変形）を一切使わない → 形状歪みゼロを保証",
    "「カプセル内部に侵入した部分」は SDF クリップで消える",
    "今月は構造完成まで。来月から動きを付ける (M2)",
], size=18)

# 右側に最終形 v16
img1 = os.path.join(SHOTS, "v16_clip_test_front.png")
add_image_safe(s, img1, Inches(9.0), Inches(2.0), w=Inches(3.8))
add_caption(s, Inches(9.0), Inches(5.4), Inches(3.8), "v16 最終形 (4/28)")

# ============================================================
# Slide 3: 計画したこと (ビジュアル設計の出発点)
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s)
add_band(s, PINK)
add_text(s, Inches(0.5), Inches(0.05), Inches(12), Inches(0.5),
         "計画したこと — そもそも何を作ろうとしていたか", size=22, bold=True, color=RGBColor(0xFF, 0xFF, 0xFF))

# 左: 手描きのデザイン (IMG_2331)
add_image_safe(s, os.path.join(CHAR, "IMG_2331.JPG"),
               Inches(0.4), Inches(0.85), h=Inches(5.4))
add_caption(s, Inches(0.4), Inches(6.3), Inches(4.0),
            "①手描き設計ノート (4 月初め)")

# 中: Gemini 3D 参照
add_image_safe(s, os.path.join(CHAR, "Gemini_Generated_Image_da978vda978vda97.png"),
               Inches(4.7), Inches(0.85), h=Inches(5.4))
add_caption(s, Inches(4.7), Inches(6.3), Inches(3.4),
            "②AI で 3D 参照画像を生成 (前/横/後/開状態)")

# 右: 仕様まとめ
add_text(s, Inches(8.5), Inches(0.85), Inches(4.5), Inches(0.7),
         "EGGYBOT の仕様", size=18, bold=True, color=PINK)
add_bullets(s, Inches(8.5), Inches(1.45), Inches(4.5), Inches(2.4), [
    "ガチャカプセル型マスコット",
    "形態 A: 雪だるま (頭+胴体)",
    "形態 B: 頭が胴体に収納 (ガチャ閉)",
    "ピンク不透明 + クリア (上半分)",
    "つるつる射出成形プラスチック",
    "短いずんぐり手足",
], size=12)

add_text(s, Inches(8.5), Inches(4.0), Inches(4.5), Inches(0.7),
         "計画したギミック", size=18, bold=True, color=PINK)
add_bullets(s, Inches(8.5), Inches(4.6), Inches(4.5), Inches(2.5), [
    "カプセル開閉 → 頭が pop up (M3 用)",
    "着せ替え (シルクハットなど)",
    "デス時に「パクパク」分裂",
    "ぴょこぴょこ idle / 歩行",
], size=12)


# ============================================================
# Slide 4: 心境
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s)
add_band(s, PINK)
add_text(s, Inches(0.5), Inches(0.05), Inches(12), Inches(0.5),
         "心境", size=24, bold=True, color=RGBColor(0xFF, 0xFF, 0xFF))

add_text(s, Inches(0.5), Inches(1.0), Inches(12), Inches(0.7),
         "「キャラ作りに 1 ヶ月」これは長すぎたか？", size=26, bold=True, color=GRAY_D)

add_bullets(s, Inches(0.7), Inches(2.0), Inches(11.8), Inches(4.5), [
    "正直、4 月の予定は「ネットワーク疎通 + プレイヤー移動 + 縮む島プロト」だった。",
    "キャラビジュアルに沼りすぎた感は否めない。M1 の網羅性は犠牲にした。",
    "ただ、「自分のゲームに自分が誇れるキャラがいる」ことは、",
    "    モチベーション保持の核 — 4 ヶ月先まで保たせるための投資と判断。",
    "AI と協働しても、最終的な「いい/変」の判断は自分でやるしかないと痛感。",
    "失敗回数は多いが、5 月以降の M2-M6 で再発する種類のものではない。",
    "5 月 (M2) からはキャラを触らず、スキル / ドラフト UI / ラウンド構造に集中する。",
], size=17)

# ============================================================
# Slide 4: 全体タイムライン
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s)
add_band(s, PINK)
add_text(s, Inches(0.5), Inches(0.05), Inches(12), Inches(0.5),
         "1 ヶ月のイテレーション (v10 → v16)", size=24, bold=True, color=RGBColor(0xFF, 0xFF, 0xFF))

stages = [
    ("v10", "4/19", "Bridge Edge Loops で manifold 化", PINK_L),
    ("v11", "4/20", "手足 organic 化 + カプセル開閉 rig", PINK_L),
    ("v12", "4/21", "Subsurf + interior fix", PINK_L),
    ("v13", "4/22", "内壁貫通穴 fix + rim crease", PINK_L),
    ("v14", "4/23", "bisect で seam 根絶", PINK_L),
    ("v15", "4/24", "Metaball で「一つの粘土」body", PINK),
    ("v15.17", "4/25", "SDF clip 採用", PINK),
    ("v15.18", "4/27", "Rigid hand parent", PINK),
    ("v16", "4/28", "構造刷新 + Blender/Unity 分業", ACCENT),
]
y0 = Inches(1.4)
xstep = (SW - Inches(1.4)) / len(stages)
for i, (ver, date, desc, col) in enumerate(stages):
    cx = Inches(0.7) + xstep * i
    box = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, cx, y0, xstep - Inches(0.1), Inches(2.4))
    box.fill.solid(); box.fill.fore_color.rgb = col
    box.line.fill.background()
    add_text(s, cx, y0 + Inches(0.15), xstep - Inches(0.1), Inches(0.5),
             ver, size=18, bold=True, color=GRAY_D, align=PP_ALIGN.CENTER)
    add_text(s, cx, y0 + Inches(0.65), xstep - Inches(0.1), Inches(0.4),
             date, size=11, color=GRAY_D, align=PP_ALIGN.CENTER)
    add_text(s, cx + Inches(0.05), y0 + Inches(1.05), xstep - Inches(0.2), Inches(1.3),
             desc, size=11, color=GRAY_D, align=PP_ALIGN.CENTER)

add_text(s, Inches(0.5), Inches(4.3), Inches(12.5), Inches(0.5),
         "→ 16 イテレーション、80+ commit。後半 (v15-v16) で「形を作る」から「構造を作る」に方針転換。",
         size=15, color=GRAY_D, align=PP_ALIGN.CENTER, bold=True)

# ============================================================
# Slide 5: ビフォー (v15 の引き伸ばし問題)
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s)
add_band(s, PINK)
add_text(s, Inches(0.5), Inches(0.05), Inches(12), Inches(0.5),
         "ビフォー: v15 の「引き伸ばされてる」問題", size=24, bold=True, color=RGBColor(0xFF, 0xFF, 0xFF))

add_image_safe(s, os.path.join(SHOTS, "v15_18_view_occlusion.png"),
               Inches(0.7), Inches(1.0), w=Inches(5.5))
add_caption(s, Inches(0.7), Inches(5.0), Inches(5.5),
            "v15.18 — 体の中に手が透けて見える + 形状が微妙に歪む")

add_text(s, Inches(7.0), Inches(1.0), Inches(5.8), Inches(0.7),
         "症状", size=20, bold=True, color=PINK)
add_bullets(s, Inches(7.0), Inches(1.6), Inches(5.8), Inches(2.5), [
    "手の形が「引き伸ばされた」ような違和感",
    "カプセル越しに「裏側の手」が透けて見える",
    "見る角度によって見え方が変わる",
    "Game / Scene view で表示が一致しない",
], size=15)
add_text(s, Inches(7.0), Inches(4.2), Inches(5.8), Inches(0.7),
         "原因", size=20, bold=True, color=PINK)
add_bullets(s, Inches(7.0), Inches(4.8), Inches(5.8), Inches(2.5), [
    "SkinnedMeshRenderer の bind-pose 補間で微小に変形",
    "shader trick (stencil / view-occ) は形状不一致 + Cull Back で原理的に不完全",
    "「形状で解決」に方針転換 → v16 へ",
], size=15)

# ============================================================
# Slide 6: アフター (v16 のクリーンな見え)
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s)
add_band(s, PINK)
add_text(s, Inches(0.5), Inches(0.05), Inches(12), Inches(0.5),
         "アフター: v16 で根治", size=24, bold=True, color=RGBColor(0xFF, 0xFF, 0xFF))

add_image_safe(s, os.path.join(SHOTS, "v16_clip_test_front.png"),
               Inches(0.7), Inches(1.0), w=Inches(5.5))
add_caption(s, Inches(0.7), Inches(5.0), Inches(5.5),
            "v16 (4/28) — 引き伸ばしゼロ、内部透けゼロ、見た目クリーン")

add_text(s, Inches(7.0), Inches(1.0), Inches(5.8), Inches(0.7),
         "やったこと", size=20, bold=True, color=PINK)
add_bullets(s, Inches(7.0), Inches(1.6), Inches(5.8), Inches(2.5), [
    "胴体だけ Blender (BodyLower / BodyUpperClear)",
    "手足は Unity primitive Sphere — Blender 介さない",
    "Hand_L_Rigid を bone の child に Transform parent",
    "(SkinnedMeshRenderer を一切使わない)",
    "SDF クリップ shader でカプセル内部を discard",
], size=15)
add_text(s, Inches(7.0), Inches(4.2), Inches(5.8), Inches(0.7),
         "効果", size=20, bold=True, color=PINK)
add_bullets(s, Inches(7.0), Inches(4.8), Inches(5.8), Inches(2.5), [
    "Skinning 経由しない → 形状歪み「原理的にゼロ」",
    "視点・角度に関係なく一貫した見え",
    "ユーザー (自分) も 「素晴らしい、変に伸ばされてない」 と承認",
], size=15)

# ============================================================
# Slide 8: 良かったこと
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s)
add_band(s, PINK)
add_text(s, Inches(0.5), Inches(0.05), Inches(12), Inches(0.5),
         "良かったこと", size=24, bold=True, color=RGBColor(0xFF, 0xFF, 0xFF))

add_text(s, Inches(0.5), Inches(1.0), Inches(12.5), Inches(0.7),
         "「やってよかった」と心から言える 5 つ", size=22, bold=True, color=GRAY_D)

goods = [
    ("MCP / Skill 全活用",          "Unity MCP + Blender MCP + Meshy MCP を業務フロー化。手作業のクリック地獄を回避。"),
    ("SDF クリップの採用",         "shader trick の沼から脱出。「形状＋ shader で根治」のパターンを獲得。"),
    ("Rigid Transform parent",       "SkinnedMeshRenderer の罠を回避する代替手段を発明・採用。skinning 歪みゼロ。"),
    ("Blender / Unity 分業",         "胴体 = Blender、手足 = Unity primitive。それぞれの強みを活かす役割分担。"),
    ("Mixamo 互換 humanoid rig",     "「Mixamo はもう使えない」と思った状態から「無加工で流せる」状態へ復帰。"),
]
y = Inches(2.0)
for title, body in goods:
    box = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.5), y, Inches(12.3), Inches(0.85))
    box.fill.solid(); box.fill.fore_color.rgb = PINK_L
    box.line.fill.background()
    add_text(s, Inches(0.7), y + Inches(0.1), Inches(3.5), Inches(0.6),
             title, size=15, bold=True, color=GRAY_D)
    add_text(s, Inches(4.3), y + Inches(0.1), Inches(8.4), Inches(0.7),
             body, size=12, color=GRAY_D)
    y += Inches(1.0)

# ============================================================
# Slide 9: 悪かったこと / 反省
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s)
add_band(s, PINK)
add_text(s, Inches(0.5), Inches(0.05), Inches(12), Inches(0.5),
         "悪かったこと / 反省", size=24, bold=True, color=RGBColor(0xFF, 0xFF, 0xFF))

add_text(s, Inches(0.5), Inches(1.0), Inches(12.5), Inches(0.7),
         "次に活かす 5 つの教訓", size=22, bold=True, color=GRAY_D)

bads = [
    ("時間の使いすぎ",               "M1 マイルストーンに対してキャラに 1 ヶ月使った。ネットワーク疎通や縮む島プロトに着手できず。"),
    ("勘で iterate しすぎた",        "海外 artist の workflow を最初に調べていれば、v10〜v14 の試行錯誤は短縮できた。"),
    ("AI に判断を任せすぎた瞬間",     "視覚審美の判断を AI に丸投げした結果、何度も「ピンク化」「色ズレ」事故が起きた。"),
    ("Shader trick の沼",            "stencil / view-occ で「もう少しで治る」と思って深掘り → 結局形状で解決すべきだった。"),
    ("分業ルールが遅かった",         "「不安なところは自分」「機械的なところは AI」を明文化したのが今日 (4/28)。最初からやるべき。"),
]
y = Inches(2.0)
for title, body in bads:
    box = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.5), y, Inches(12.3), Inches(0.85))
    box.fill.solid(); box.fill.fore_color.rgb = RGBColor(0xFF, 0xE5, 0xCC)
    box.line.fill.background()
    add_text(s, Inches(0.7), y + Inches(0.1), Inches(3.5), Inches(0.6),
             title, size=15, bold=True, color=GRAY_D)
    add_text(s, Inches(4.3), y + Inches(0.1), Inches(8.4), Inches(0.7),
             body, size=12, color=GRAY_D)
    y += Inches(1.0)

# ============================================================
# Slide 10: 発見・知見
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s)
add_band(s, PINK)
add_text(s, Inches(0.5), Inches(0.05), Inches(12), Inches(0.5),
         "発見・知見 (技術)", size=24, bold=True, color=RGBColor(0xFF, 0xFF, 0xFF))

discoveries = [
    ("01", "SkinnedMeshRenderer の bind-pose 補間で微小な歪みが出る (100% bone weight でも)。"),
    ("02", "Stencil clipping は描画順 + screen-space 依存で URP forward では信頼性に欠ける。"),
    ("03", "SDF discard は描画順非依存・カメラ角度非依存・SRP Batcher 完全互換。"),
    ("04", "MaterialPropertyBlock は SRP Batcher 有効時に黙殺される (global uniform を使う)。"),
    ("05", "Humanoid Avatar の muscle space は bone scale 違いを吸収する (人間 1.9m → 球体 0.7m でも OK)。"),
    ("06", "rigid Transform parent は bone が静止していれば Mixamo bone axis convention を完全無視できる。"),
    ("07", "Solidify modifier の薄殻 + Cull Back は「上から覗くと内部が空洞に見える」事故を起こす。"),
    ("08", "Blender → FBX export の global_scale / apply_unit_scale 設定はキャラ size 罠の温床。"),
    ("09", ".blend を直接編集 → FBX 上書き export が、「ユーザー手動編集を保つ」唯一の安全パス。"),
    ("10", "MCP (Unity / Blender / Meshy) を組み合わせると iteration 速度が体感 5 倍以上になる。"),
]
y = Inches(1.1)
for num, body in discoveries:
    add_text(s, Inches(0.5), y, Inches(0.7), Inches(0.4),
             num, size=14, bold=True, color=PINK, align=PP_ALIGN.CENTER)
    add_text(s, Inches(1.1), y, Inches(11.8), Inches(0.5),
             body, size=12, color=GRAY_D)
    y += Inches(0.55)

# ============================================================
# Slide 11: 数字で見る 4 月
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s)
add_band(s, PINK)
add_text(s, Inches(0.5), Inches(0.05), Inches(12), Inches(0.5),
         "数字で見る 4 月", size=24, bold=True, color=RGBColor(0xFF, 0xFF, 0xFF))

stats = [
    ("16",   "イテレーション", "v10 → v16"),
    ("80+",  "Commit",         "main ブランチ"),
    ("244",  "Screenshot",     "Assets/Screenshots/"),
    ("3",    "MCP server",     "Unity / Blender / Meshy"),
    ("2",    "Sandbox scene",  "v16 / v16_humanoid"),
    ("0",    "ピンク化事故",   "新規発生 (運用ルール後)"),
]
xstep = SW / 3
ystep = Inches(2.4)
for i, (n, label, sub) in enumerate(stats):
    col = i % 3
    row = i // 3
    cx = xstep * col + Inches(0.3)
    cy = Inches(1.3) + ystep * row
    box = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, cx, cy, xstep - Inches(0.6), Inches(2.0))
    box.fill.solid(); box.fill.fore_color.rgb = PINK_L
    box.line.fill.background()
    add_text(s, cx, cy + Inches(0.25), xstep - Inches(0.6), Inches(0.9),
             n, size=58, bold=True, color=PINK, align=PP_ALIGN.CENTER)
    add_text(s, cx, cy + Inches(1.15), xstep - Inches(0.6), Inches(0.4),
             label, size=14, bold=True, color=GRAY_D, align=PP_ALIGN.CENTER)
    add_text(s, cx, cy + Inches(1.55), xstep - Inches(0.6), Inches(0.4),
             sub, size=11, color=GRAY_L, align=PP_ALIGN.CENTER)

# ============================================================
# Slide 12: 今日の進捗 (4/28 step1)
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s)
add_band(s, ACCENT)
add_text(s, Inches(0.5), Inches(0.05), Inches(12), Inches(0.5),
         "今日 (4/28) の進捗 — 透明 Humanoid Rig 構築", size=24, bold=True, color=GRAY_D)

add_image_safe(s, os.path.join(SHOTS, "v16_humanoid_step1_front.png"),
               Inches(0.7), Inches(1.1), w=Inches(5.5))
add_caption(s, Inches(0.7), Inches(5.1), Inches(5.5),
            "v16 humanoid Step 1 完了 (Empty Transform で見た目変わらず)")

add_text(s, Inches(7.0), Inches(1.1), Inches(5.8), Inches(0.7),
         "今日構築したもの", size=20, bold=True, color=PINK)
add_bullets(s, Inches(7.0), Inches(1.7), Inches(5.8), Inches(2.5), [
    "Sandbox_v16_humanoid.unity 新規作成",
    "EggybotHumanoidRig (空 GameObject)",
    "26 個の mixamorig:* bone を T-pose 配置",
    "Mixamo 1.9m × 0.37 = v16 0.7m サイズ",
    "全 bone Empty Transform — 見た目には不可視",
], size=14)
add_text(s, Inches(7.0), Inches(4.5), Inches(5.8), Inches(0.7),
         "次のステップ", size=20, bold=True, color=PINK)
add_bullets(s, Inches(7.0), Inches(5.1), Inches(5.8), Inches(2.5), [
    "Step 2: Avatar 構築 (HumanDescription + AvatarBuilder)",
    "Step 3: 球と body を bone child に再 parent",
    "Step 4: Mixamo Walking 流す",
], size=14)

# ============================================================
# Slide 13: M2 以降の見通し
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s)
add_band(s, PINK)
add_text(s, Inches(0.5), Inches(0.05), Inches(12), Inches(0.5),
         "M2 以降の見通し", size=24, bold=True, color=RGBColor(0xFF, 0xFF, 0xFF))

add_text(s, Inches(0.5), Inches(1.0), Inches(12.5), Inches(0.7),
         "5 月から本来の M1 タスクと M2 を並行で進める", size=22, bold=True, color=GRAY_D)

milestones = [
    ("M2 (5月)", "Mixamo 統合完了 / スキル 3 種 / ドラフト UI / 4 ラウンド構造"),
    ("M3 (6月)", "カプセル開閉 anim / しっぽ狩りバトル / 爆弾鬼"),
    ("M4 (7月)", "しっぽ狩り逃走 / スキル 10 種 / ルールカード 4 種"),
    ("M5 (8月)", "シーズンメタ / MVP 演出 / バランス調整"),
    ("M6 (9月)", "QA / 最適化 / Steam ビルド / 発表"),
]
y = Inches(2.0)
for label, body in milestones:
    box = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.5), y, Inches(12.3), Inches(0.7))
    box.fill.solid(); box.fill.fore_color.rgb = PINK_L if "M2" in label else BG
    box.line.color.rgb = PINK_L
    add_text(s, Inches(0.7), y + Inches(0.13), Inches(2.0), Inches(0.5),
             label, size=15, bold=True, color=PINK)
    add_text(s, Inches(2.8), y + Inches(0.13), Inches(9.7), Inches(0.5),
             body, size=13, color=GRAY_D)
    y += Inches(0.85)

add_text(s, Inches(0.5), Inches(6.5), Inches(12.5), Inches(0.5),
         "→ キャラには触れない。「動く」ことに集中。", size=14,
         color=GRAY_D, align=PP_ALIGN.CENTER, bold=True)

# ============================================================
# Slide 14: 最後に — 心境 (まとめ)
# ============================================================
s = prs.slides.add_slide(BLANK)
add_bg(s, PINK_L)
add_text(s, Inches(0.5), Inches(0.8), Inches(12), Inches(1.0),
         "最後に", size=44, bold=True, color=GRAY_D, align=PP_ALIGN.CENTER)

msg = (
    "「キャラ作りに 1 ヶ月」は、客観的に見れば長すぎた。\n"
    "でも、自分のゲームに自分が誇れるキャラがいるという感覚は、\n"
    "残り 5 ヶ月のモチベーション保持の核になる。\n"
    "\n"
    "AI と協働しても、最後の判断は自分。\n"
    "それを 1 ヶ月かけて学べたのは、5 月以降に効く。\n"
    "\n"
    "5 月からは、キャラを触らない。動かす。"
)
add_text(s, Inches(1.5), Inches(2.5), Inches(10.3), Inches(4.0),
         msg, size=22, color=GRAY_D, align=PP_ALIGN.CENTER)

add_text(s, Inches(0.5), Inches(6.8), Inches(12), Inches(0.5),
         "ChaosDraftParty / 2026-04-28", size=14, color=GRAY_L, align=PP_ALIGN.CENTER)


# Save
prs.save(OUT)
print(f"saved: {OUT}")
print(f"slides: {len(prs.slides)}")
