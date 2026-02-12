FROM pytorch/pytorch:2.4.0-cuda12.4-cudnn9-runtime

# Install system dependencies
RUN apt-get update && apt-get install -y \
    git \
    wget \
    && rm -rf /var/lib/apt/lists/*

# Install brotli implementations FIRST (aiohttp checks for these at import time)
RUN pip install --no-cache-dir Brotli brotlicffi

# Install Python packages - install TotalSegmentator without deps first, then add what's needed
# This prevents TotalSegmentator from overwriting PyTorch with an incompatible version
RUN pip install --no-cache-dir \
    nibabel \
    numpy \
    numpy-stl \
    scikit-image \
    SimpleITK

# Install TotalSegmentator (will use the already installed PyTorch)
RUN pip install --no-cache-dir TotalSegmentator

# Verify PyTorch CUDA is working
RUN python -c "import torch; print(f'PyTorch: {torch.__version__}'); print(f'CUDA available: {torch.cuda.is_available()}')"

# Install aiohttp with speedups first, then runpod
RUN pip install --no-cache-dir "aiohttp[speedups]>=3.9.3"
RUN pip install --no-cache-dir runpod

# Force reinstall aiohttp to ensure it picks up brotli
RUN pip install --no-cache-dir --force-reinstall --no-deps "aiohttp[speedups]"

# Verify brotli is properly detected by aiohttp
RUN python -c "import brotli; print('✓ brotli imported successfully')" && \
    python -c "import aiohttp; print(f'✓ aiohttp version: {aiohttp.__version__}')" && \
    python -c "from aiohttp.http_parser import HAS_BROTLI; print(f'✓ aiohttp HAS_BROTLI: {HAS_BROTLI}')" && \
    python -c "from aiohttp import hdrs; print('✓ aiohttp headers module available')"

# Download model weights during build (faster cold starts)
RUN python -c "from totalsegmentator.python_api import totalsegmentator; print('Weights will download on first run')"

# Copy handler
COPY handler.py /handler.py

# Set entrypoint
CMD ["python", "-u", "/handler.py"]
