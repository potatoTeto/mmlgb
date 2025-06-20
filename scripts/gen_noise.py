import math
import os

THRESHOLD = 50

def freq(r, s):
    if r == 0:
        r = 0.5
    return 524228 / r / math.pow(2, s + 1)

def bitmask(r, s):
    return (s * 16) + r

# Prepare C source content
c_lines = []
c_lines.append('#include "noisefreq.h"\n\n')
c_lines.append("const UBYTE noise_freq[] = {\n")

# Main noise table (8 octaves)
for r in range(7, -1, -1):
    row = []
    for s in range(15, 3, -1):
        row.append(f"{bitmask(r, s)}U")
    row.extend(["0U"] * 4)
    c_lines.append("\t" + ", ".join(row) + ",\n")

# Extended table: o9
extended_o9 = []
for i in range(3):
    for j in range(4):
        extended_o9.append(f"{56 - i * 16 - j - 1}U")
extended_o9.extend(["0U"] * 4)
c_lines.append("\t" + ", ".join(extended_o9) + ",\n")

# Extended table: o10
extended_o10 = [f"{max(1, 7 - i)}U" for i in range(12)]
c_lines.append("\t" + ", ".join(extended_o10) + "\n")

c_lines.append("};\n")

# Prepare header content
h_lines = [
    "#ifndef MUS_NOISEFREQ_H\n",
    "#define MUS_NOISEFREQ_H\n\n",
    "#include <gbdk/platform.h>\n\n",
    "#define MUS_NOISE_FIRST_OCTAVE 1U\n\n",
    "extern const UBYTE noise_freq[];\n\n",
    "#endif\n"
]

# Write files to ../driver
os.makedirs("../driver", exist_ok=True)

with open("../driver/noisefreq.c", "w") as f_c:
    f_c.writelines(c_lines)

with open("../driver/noisefreq.h", "w") as f_h:
    f_h.writelines(h_lines)
