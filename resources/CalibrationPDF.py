from reportlab.lib.pagesizes import A4
from reportlab.lib.units import cm
from reportlab.pdfgen import canvas
from reportlab.lib.colors import blue, black, red, HexColor
import math

robot_length = 9.5  # cm
robot_width = 9.7   # cm

page_width, page_height = A4  # in points (1cm = 28.35 pt, handled by 'cm' unit)

# ─────────────────────────────────────────────────────────────────────────────
# BLATT 1: Strecke
# ─────────────────────────────────────────────────────────────────────────────
c = canvas.Canvas('output/Zumo_Calibration_Strecke.pdf', pagesize=A4)

# Titel
c.setFont("Helvetica-Bold", 18)
c.drawCentredString(page_width / 2, page_height - 1.5*cm, "Zumo Kalibrierung: Strecke")
c.setFont("Helvetica", 11)
c.drawCentredString(page_width / 2, page_height - 2.5*cm, "Blaue Striche alle 4 cm  |  Beim Drucken: Seitengrösse auf 100% stellen!")

# 5 blaue Querstriche alle 4 cm (inkl. 0-Punkt = Startlinie)
stripe_half_width = 6.0 * cm
for i in range(6):
    y = start_y + i * 4.0 * cm
    c.setStrokeColor(blue)
    c.setLineWidth(10)
    c.line(line_x - stripe_half_width, y, line_x + stripe_half_width, y)

    # Beschriftung
    c.setFillColor(black)
    c.setFont("Helvetica-Bold", 12)
    c.drawString(line_x + stripe_half_width + 0.3*cm, y - 0.2*cm, f"{i * 4} cm")

# Roboter Startposition (gestrichelte rote Box unter dem 0-Strich)
box_x = line_x - (robot_width / 2) * cm
box_y = start_y - robot_length * cm
c.setStrokeColor(red)
c.setLineWidth(1)
c.setDash(6, 3)
c.rect(box_x, box_y, robot_width * cm, robot_length * cm)
c.setDash()  # Reset dash

c.setFillColor(red)
c.setFont("Helvetica", 9)
c.drawCentredString(line_x, box_y - 0.4*cm, f"Startposition Roboter ({robot_width}x{robot_length} cm)")

c.save()
print("Strecke PDF gespeichert.")

# ─────────────────────────────────────────────────────────────────────────────
# BLATT 2: Drehen an Ort
# ─────────────────────────────────────────────────────────────────────────────
c = canvas.Canvas('output/Zumo_Calibration_Kreis.pdf', pagesize=A4)

# Titel
c.setFont("Helvetica-Bold", 18)
c.drawCentredString(page_width / 2, page_height - 1.5*cm, "Zumo Kalibrierung: Drehen an Ort")
c.setFont("Helvetica", 11)
c.drawCentredString(page_width / 2, page_height - 2.5*cm, "Blaue Striche alle 90°  |  Beim Drucken: Seitengrösse auf 100% stellen!")

# Kreismittelpunkt auf der Seite
cx = page_width / 2
cy = page_height / 2 - 1.0 * cm

# Kreuz-Fadenkreuz in der Mitte
c.setStrokeColor(black)
c.setLineWidth(1)
c.line(cx - 0.7*cm, cy, cx + 0.7*cm, cy)
c.line(cx, cy - 0.7*cm, cx, cy + 0.7*cm)

# Blaue Striche bei 0°, 90°, 180°, 270°
# Sensor befindet sich an der Vorderkante (ca. 4.75cm vom Zentrum)
# Striche starten ab 3.5cm vom Zentrum, enden bei 9cm
inner_r = 3.5 * cm
outer_r = 9.0 * cm

# Dünne Hilfslinien (schwarz, gepunktet) den ganzen Weg zum Rand
for angle_deg in [0, 90, 180, 270]:
    rad = math.radians(angle_deg)
    sx_help = cx + math.cos(rad) * 0.8 * cm
    sy_help = cy + math.sin(rad) * 0.8 * cm
    ex_help = cx + math.cos(rad) * outer_r
    ey_help = cy + math.sin(rad) * outer_r
    c.setStrokeColor(HexColor('#AAAAAA'))
    c.setLineWidth(1)
    c.setDash(4, 4)
    c.line(sx_help, sy_help, ex_help, ey_help)
    c.setDash()

# Dicke blaue Striche (senkrecht zur Fahrtlinie, damit Sensor sie gut trifft)
stripe_half = 1.5 * cm
for angle_deg in [0, 90, 180, 270]:
    rad = math.radians(angle_deg)
    perp_rad = rad + math.pi / 2  # Senkrechte Richtung

    # Mittelpunkt des Striches
    mid_r = (inner_r + outer_r) / 2
    mid_x = cx + math.cos(rad) * mid_r
    mid_y = cy + math.sin(rad) * mid_r

    # Endpunkte des Querstriches
    x1 = mid_x + math.cos(perp_rad) * stripe_half
    y1 = mid_y + math.sin(perp_rad) * stripe_half
    x2 = mid_x - math.cos(perp_rad) * stripe_half
    y2 = mid_y - math.sin(perp_rad) * stripe_half

    c.setStrokeColor(blue)
    c.setLineWidth(10)
    c.line(x1, y1, x2, y2)

    # Winkelbeschriftung
    label_r = outer_r + 1.0 * cm
    lx = cx + math.cos(rad) * label_r
    ly = cy + math.sin(rad) * label_r
    c.setFillColor(black)
    c.setFont("Helvetica-Bold", 14)
    c.drawCentredString(lx, ly - 0.2*cm, f"{angle_deg}°")

# Startposition des Roboters (gestrichelte rote Box in der Mitte)
box_x = cx - (robot_width / 2) * cm
box_y = cy - (robot_length / 2) * cm
c.setStrokeColor(red)
c.setLineWidth(1)
c.setDash(6, 3)
c.rect(box_x, box_y, robot_width * cm, robot_length * cm)
c.setDash()

c.setFillColor(red)
c.setFont("Helvetica", 9)
c.drawCentredString(cx, box_y - 0.5*cm, f"Roboter zentrieren. Oberkante = 0°")

c.save()
print("Kreis PDF gespeichert.")