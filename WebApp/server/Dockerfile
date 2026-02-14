FROM python:3.11-slim

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    wget xz-utils libxi6 libxxf86vm1 libxfixes3 libxrender1 libgl1 \
    libxkbcommon0 libsm6 \
    && rm -rf /var/lib/apt/lists/*

# Install Blender for headless 3D processing (STL → FBX etc.)
RUN wget -q https://download.blender.org/release/Blender4.0/blender-4.0.2-linux-x64.tar.xz \
    && tar xf blender-4.0.2-linux-x64.tar.xz -C /opt/ \
    && ln -s /opt/blender-4.0.2-linux-x64/blender /usr/local/bin/blender \
    && rm blender-4.0.2-linux-x64.tar.xz

RUN pip install --no-cache-dir \
    fastapi \
    uvicorn[standard] \
    python-multipart \
    qrcode[pil] \
    reportlab \
    httpx \
    nibabel \
    scipy \
    scikit-image \
    numpy-stl

COPY main.py .
COPY app/ ./app/
COPY assets/ ./assets/

RUN mkdir -p /app/data

EXPOSE 8000

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
