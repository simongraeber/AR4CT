"""QR code image and printable PDF generation."""

import io

from fastapi import APIRouter, HTTPException
from fastapi.responses import StreamingResponse

import qrcode
from PIL import Image, ImageDraw

from app.config import PUBLIC_BASE_URL, TOOL_IMAGE_PATH
from app.storage import scan_exists

router = APIRouter(prefix="/scans", tags=["qr"])


# ── Helpers ──────────────────────────────────────────────────────────────

def generate_qr_with_border(url: str) -> Image.Image:
    """
    Generate a QR code with a thick coloured border and a different colour
    square at each corner, giving Unity's image-tracking far more distinctive
    visual features to lock onto.
    """
    qr = qrcode.QRCode(
        version=None,
        error_correction=qrcode.constants.ERROR_CORRECT_L,
        box_size=10,
        border=4,
    )
    qr.add_data(url)
    qr.make(fit=True)
    qr_img = qr.make_image(fill_color="black", back_color="white").convert("RGB")

    qr_w, qr_h = qr_img.size

    border = 40
    corner_size = 60

    total_w = qr_w + 2 * border
    total_h = qr_h + 2 * border

    canvas = Image.new("RGB", (total_w, total_h), "white")
    draw = ImageDraw.Draw(canvas)

    draw.rectangle([0, 0, total_w - 1, total_h - 1], outline="black", width=border)
    canvas.paste(qr_img, (border, border))

    corners = {
        "top_left":     ("red",    (0, 0, corner_size, corner_size)),
        "top_right":    ("blue",   (total_w - corner_size, 0, total_w, corner_size)),
        "bottom_left":  ("green",  (0, total_h - corner_size, corner_size, total_h)),
        "bottom_right": ("yellow", (total_w - corner_size, total_h - corner_size, total_w, total_h)),
    }
    for _name, (color, rect) in corners.items():
        draw.rectangle(rect, fill=color)

    return canvas


# ── Endpoints ────────────────────────────────────────────────────────────

@router.get("/{scan_id}/image.png")
async def get_qr_code(scan_id: str):
    """Generate a QR code PNG that encodes the AR viewer URL for this scan."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    url = f"{PUBLIC_BASE_URL}/app/{scan_id}"
    img = generate_qr_with_border(url)

    buf = io.BytesIO()
    img.save(buf, format="PNG")
    buf.seek(0)

    return StreamingResponse(
        buf,
        media_type="image/png",
        headers={"Content-Disposition": f'inline; filename="{scan_id}_qr.png"'},
    )


@router.get("/{scan_id}/print.pdf")
async def get_print_pdf(scan_id: str):
    """
    Generate a printable A4 PDF containing the QR code (15 × 15 cm) and
    the tool image (5 × 5 cm).
    """
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    from reportlab.lib.pagesizes import A4
    from reportlab.lib.units import cm
    from reportlab.pdfgen import canvas as pdf_canvas
    from reportlab.lib.utils import ImageReader

    url = f"{PUBLIC_BASE_URL}/app/{scan_id}"
    qr_img = generate_qr_with_border(url)

    qr_buf = io.BytesIO()
    qr_img.save(qr_buf, format="PNG")
    qr_buf.seek(0)

    pdf_buf = io.BytesIO()
    page_w, page_h = A4

    c = pdf_canvas.Canvas(pdf_buf, pagesize=A4)
    c.setTitle(f"AR4CT Scan – {scan_id}")

    qr_size = 15 * cm
    qr_x = (page_w - qr_size) / 2
    qr_y = page_h - 4 * cm - qr_size

    c.drawImage(ImageReader(qr_buf), qr_x, qr_y, width=qr_size, height=qr_size)

    c.setFont("Helvetica", 9)
    c.drawCentredString(page_w / 2, qr_y - 0.5 * cm, url)

    tool_size = 5 * cm
    tool_x = (page_w - tool_size) / 2
    tool_y = qr_y - 1.5 * cm - tool_size

    if TOOL_IMAGE_PATH.exists():
        c.drawImage(
            ImageReader(str(TOOL_IMAGE_PATH)),
            tool_x,
            tool_y,
            width=tool_size,
            height=tool_size,
            preserveAspectRatio=True,
            mask="auto",
        )

    text_y = tool_y - 0.8 * cm
    c.setFont("Helvetica-Bold", 11)
    c.drawCentredString(page_w / 2, text_y, "Cut out the tool marker and tape it to your tool.")
    text_y -= 0.5 * cm
    c.setFont("Helvetica", 10)
    c.drawCentredString(
        page_w / 2, text_y,
        "The AR app will show the distance from your tool (the marker)",
    )
    text_y -= 0.4 * cm
    c.drawCentredString(page_w / 2, text_y, "to the target point inside the object.")

    c.save()
    pdf_buf.seek(0)

    return StreamingResponse(
        pdf_buf,
        media_type="application/pdf",
        headers={"Content-Disposition": f'inline; filename="{scan_id}_print.pdf"'},
    )
