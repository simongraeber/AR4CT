import runpod
import os
import uuid
import base64
import nibabel as nib
import numpy as np
from skimage import measure
from stl import mesh
import tempfile
import zipfile
import glob
import requests
import SimpleITK as sitk
import subprocess
import sys
import signal
import threading
import time

# Startup logging to verify dependencies
print("=" * 60)
print("TotalSegmentator Handler Starting...")
print("=" * 60)
try:
    import brotli
    print(f"✓ brotli module: AVAILABLE (version info: {brotli.__file__})")
except ImportError as e:
    print(f"✗ brotli module: NOT AVAILABLE - {e}")

try:
    import aiohttp
    print(f"✓ aiohttp version: {aiohttp.__version__}")
    # Check if aiohttp actually detected brotli
    try:
        from aiohttp.http_parser import HAS_BROTLI
        print(f"✓ aiohttp HAS_BROTLI flag: {HAS_BROTLI}")
    except ImportError:
        print("✗ Could not check aiohttp HAS_BROTLI flag")
except ImportError as e:
    print(f"✗ aiohttp: NOT AVAILABLE - {e}")

# Comprehensive GPU/CUDA diagnostics
print("=" * 60)
print("GPU/CUDA DIAGNOSTICS")
print("=" * 60)

import torch
print(f"PyTorch version: {torch.__version__}")
print(f"PyTorch file location: {torch.__file__}")

# Check if PyTorch was built with CUDA
print(f"PyTorch built with CUDA: {torch.cuda.is_available()}")
print(f"PyTorch CUDA version (compiled): {torch.version.cuda}")
print(f"PyTorch cuDNN version: {torch.backends.cudnn.version() if torch.backends.cudnn.is_available() else 'N/A'}")
print(f"cuDNN enabled: {torch.backends.cudnn.enabled}")

# Check NVIDIA driver
try:
    import subprocess
    result = subprocess.run(['nvidia-smi', '--query-gpu=name,driver_version,memory.total', '--format=csv,noheader'], 
                          capture_output=True, text=True, timeout=10)
    print(f"nvidia-smi output: {result.stdout.strip()}")
    if result.returncode != 0:
        print(f"nvidia-smi error: {result.stderr}")
except Exception as e:
    print(f"nvidia-smi failed: {e}")

# Check CUDA devices
if torch.cuda.is_available():
    print(f"CUDA device count: {torch.cuda.device_count()}")
    print(f"CUDA current device: {torch.cuda.current_device()}")
    for i in range(torch.cuda.device_count()):
        print(f"  GPU {i}: {torch.cuda.get_device_name(i)}")
        props = torch.cuda.get_device_properties(i)
        print(f"    - Total memory: {props.total_memory / 1024**3:.2f} GB")
        print(f"    - Compute capability: {props.major}.{props.minor}")
        print(f"    - Multi-processor count: {props.multi_processor_count}")
    
    # Test actual CUDA operation
    print("Testing CUDA tensor operations...")
    try:
        # Create tensor on GPU
        x = torch.randn(1000, 1000, device='cuda')
        y = torch.randn(1000, 1000, device='cuda')
        z = torch.matmul(x, y)
        torch.cuda.synchronize()  # Wait for GPU operation to complete
        print(f"✓ CUDA tensor test PASSED - result shape: {z.shape}, device: {z.device}")
        del x, y, z
        torch.cuda.empty_cache()
        print(f"✓ GPU memory after cleanup: {torch.cuda.memory_allocated() / 1024**2:.2f} MB allocated")
    except Exception as e:
        print(f"✗ CUDA tensor test FAILED: {e}")
        import traceback
        traceback.print_exc()
else:
    print("✗ CUDA is NOT available!")
    print("Possible reasons:")
    print("  - No NVIDIA GPU in system")
    print("  - NVIDIA driver not installed")
    print("  - PyTorch not built with CUDA support")
    print("  - CUDA version mismatch")

# Check environment variables that might affect CUDA
print("=" * 60)
print("CUDA-related environment variables:")
cuda_env_vars = ['CUDA_VISIBLE_DEVICES', 'CUDA_HOME', 'CUDA_PATH', 'LD_LIBRARY_PATH', 'NVIDIA_VISIBLE_DEVICES']
for var in cuda_env_vars:
    val = os.environ.get(var, 'NOT SET')
    print(f"  {var}: {val}")

# Check nnUNet environment (TotalSegmentator uses nnUNet)
print("=" * 60)
print("nnUNet environment variables:")
nnunet_vars = ['nnUNet_raw', 'nnUNet_preprocessed', 'nnUNet_results', 'TOTALSEG_HOME_DIR']
for var in nnunet_vars:
    val = os.environ.get(var, 'NOT SET')
    print(f"  {var}: {val}")

# Check TotalSegmentator and nnUNet versions
print("=" * 60)
print("Package versions:")
try:
    import totalsegmentator as ts_module
    ts_version = getattr(ts_module, '__version__', 'unknown')
    print(f"  TotalSegmentator version: {ts_version}")
    print(f"  TotalSegmentator location: {ts_module.__file__}")
    print(f"  LATEST on PyPI: 2.12.0 (as of Jan 2026)")
    if ts_version != 'unknown' and ts_version < '2.12.0':
        print(f"  ⚠ WARNING: Not using latest version!")
except Exception as e:
    print(f"  TotalSegmentator: Error getting version - {e}")

try:
    import nnunetv2
    nnunet_version = getattr(nnunetv2, '__version__', 'unknown')
    print(f"  nnUNet v2 version: {nnunet_version}")
except ImportError:
    try:
        import nnunet
        nnunet_version = getattr(nnunet, '__version__', 'unknown')
        print(f"  nnUNet v1 version: {nnunet_version}")
    except ImportError:
        print("  nnUNet: Not found")

try:
    import SimpleITK
    print(f"  SimpleITK version: {SimpleITK.__version__}")
except:
    print("  SimpleITK: Not found")

try:
    import nibabel
    print(f"  nibabel version: {nibabel.__version__}")
except:
    print("  nibabel: Not found")

print("=" * 60)

def run_totalsegmentator_subprocess(input_path: str, output_dir: str, fast: bool, device: str, timeout: int = 1800):
    """
    Run TotalSegmentator in a separate subprocess to prevent hanging.
    This ensures that if inference fails, we can kill the subprocess cleanly.
    
    Args:
        input_path: Path to input NIfTI file
        output_dir: Path to output directory
        fast: Whether to use fast mode (3mm resampling)
        device: 'gpu' or 'cpu'
        timeout: Maximum time in seconds (default 30 minutes)
    
    Returns:
        tuple: (success: bool, message: str)
    """
    # Build the command - use TotalSegmentator CLI command
    cmd = [
        "TotalSegmentator",
        "-i", input_path,
        "-o", output_dir,
        "--device", device,
        "-v"  # verbose
    ]
    
    if fast:
        cmd.append("--fast")
    
    print(f"Running TotalSegmentator subprocess: {' '.join(cmd)}")
    print(f"Timeout: {timeout} seconds")
    
    # Track GPU memory during inference
    gpu_monitor_stop = threading.Event()
    
    def monitor_gpu():
        """Background thread to monitor GPU usage"""
        import torch
        while not gpu_monitor_stop.is_set():
            if torch.cuda.is_available():
                try:
                    mem_allocated = torch.cuda.memory_allocated() / 1024**2
                    mem_reserved = torch.cuda.memory_reserved() / 1024**2
                    print(f"[GPU Monitor] Allocated: {mem_allocated:.1f} MB, Reserved: {mem_reserved:.1f} MB")
                except:
                    pass
            gpu_monitor_stop.wait(30)  # Check every 30 seconds
    
    gpu_thread = threading.Thread(target=monitor_gpu, daemon=True)
    gpu_thread.start()
    
    try:
        # Run with timeout and capture output
        process = subprocess.Popen(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            bufsize=1,  # Line buffered
            preexec_fn=os.setsid  # Create new process group for clean killing
        )
        
        # Read output in real-time with timeout
        start_time = time.time()
        output_lines = []
        
        while True:
            # Check timeout
            elapsed = time.time() - start_time
            if elapsed > timeout:
                print(f"[ERROR] TotalSegmentator timed out after {elapsed:.0f}s")
                # Kill the entire process group
                os.killpg(os.getpgid(process.pid), signal.SIGKILL)
                process.wait()
                return False, f"Timeout after {timeout} seconds"
            
            # Check if process finished
            retcode = process.poll()
            
            # Read available output (non-blocking would be better but this works)
            try:
                line = process.stdout.readline()
                if line:
                    line = line.rstrip()
                    print(f"[TotalSeg] {line}")
                    output_lines.append(line)
            except:
                pass
            
            if retcode is not None:
                # Process finished - read remaining output
                remaining = process.stdout.read()
                if remaining:
                    for line in remaining.split('\n'):
                        if line.strip():
                            print(f"[TotalSeg] {line}")
                            output_lines.append(line)
                
                if retcode == 0:
                    print(f"[SUCCESS] TotalSegmentator completed in {elapsed:.1f}s")
                    return True, "Success"
                else:
                    error_msg = f"TotalSegmentator failed with exit code {retcode}"
                    print(f"[ERROR] {error_msg}")
                    return False, error_msg
            
            # Small sleep to prevent busy-waiting
            time.sleep(0.1)
            
    except Exception as e:
        print(f"[ERROR] Exception running TotalSegmentator: {e}")
        import traceback
        traceback.print_exc()
        return False, str(e)
    finally:
        gpu_monitor_stop.set()
        # Clean up any GPU memory
        try:
            import torch
            if torch.cuda.is_available():
                torch.cuda.empty_cache()
                print(f"[GPU Cleanup] Memory after cleanup: {torch.cuda.memory_allocated() / 1024**2:.1f} MB")
        except:
            pass

def nifti_to_stl(nifti_path: str, stl_path: str):
    """Convert a NIfTI segmentation mask to STL mesh."""
    img = nib.load(nifti_path)
    data = img.get_fdata()
    
    if data.max() == 0:
        return None  # Empty mask
    
    # Generate mesh using marching cubes
    verts, faces, _, _ = measure.marching_cubes(data, level=0.5)
    
    # Transform voxel coords → physical coords (mm)
    spacing = np.array(img.header.get_zooms()[:3])
    origin = np.array(img.affine[:3, 3])
    verts = verts * spacing + origin
    
    # Create STL mesh
    stl_mesh = mesh.Mesh(np.zeros(faces.shape[0], dtype=mesh.Mesh.dtype))
    for i, face in enumerate(faces):
        for j in range(3):
            stl_mesh.vectors[i][j] = verts[face[j], :]
    
    stl_mesh.save(stl_path)
    return stl_path


def extract_body_surface(input_nifti_path: str, stl_path: str, hu_threshold: float = -500.0):
    """
    Extract the outer body surface from a CT scan and save as STL.

    Uses a Hounsfield-Unit threshold to separate body from air, then
    applies binary morphological closing to fill small holes before
    running marching cubes.

    Args:
        input_nifti_path: Path to the original CT NIfTI file.
        stl_path: Where to write the resulting STL.
        hu_threshold: HU value above which voxels are considered "body".
                      -500 captures skin, fat, muscle, bone, etc.
    Returns:
        stl_path on success, None if the mask is empty.
    """
    from scipy import ndimage

    print(f"  Extracting body surface (HU > {hu_threshold}) ...")
    img = nib.load(input_nifti_path)
    data = img.get_fdata()

    # Threshold to get a binary body mask
    body_mask = (data > hu_threshold).astype(np.uint8)

    if body_mask.max() == 0:
        print("  SKIP: body mask is empty")
        return None

    # Morphological closing to fill internal gaps and smooth the surface
    struct = ndimage.generate_binary_structure(3, 2)
    body_mask = ndimage.binary_closing(body_mask, structure=struct, iterations=3).astype(np.uint8)

    # Keep only the largest connected component (the body)
    labelled, num_features = ndimage.label(body_mask)
    if num_features > 1:
        component_sizes = ndimage.sum(body_mask, labelled, range(1, num_features + 1))
        largest = int(np.argmax(component_sizes)) + 1
        body_mask = (labelled == largest).astype(np.uint8)

    # Optional: subsample for performance if the volume is very large
    # (marching cubes on full-res CT can be huge)
    step = 1
    total_voxels = body_mask.sum()
    if total_voxels > 20_000_000:
        step = 2  # halve resolution
        print(f"  Body mask has {total_voxels} voxels – using step_size={step}")

    verts, faces, _, _ = measure.marching_cubes(body_mask, level=0.5, step_size=step)

    # Transform voxel coords → physical coords (mm)
    # marching_cubes with step_size already returns coords in original voxel space
    spacing = np.array(img.header.get_zooms()[:3])
    origin = np.array(img.affine[:3, 3])
    verts = verts * spacing + origin

    stl_mesh = mesh.Mesh(np.zeros(faces.shape[0], dtype=mesh.Mesh.dtype))
    for i, face in enumerate(faces):
        for j in range(3):
            stl_mesh.vectors[i][j] = verts[face[j], :]

    stl_mesh.save(stl_path)
    size_kb = os.path.getsize(stl_path) / 1024
    print(f"  Body surface STL: {size_kb:.1f} KB ({len(faces)} faces)")
    return stl_path

def handler(event):
    """
    RunPod serverless handler for TotalSegmentator.
    
    Input:
        - file_url: URL to download the CT file from
        - organs: List of organs to segment
        - fast: Whether to use fast mode (3mm resampling)
        - callback_url: Base URL to upload STL files to (e.g., https://api.ar4ct.com/scans/{scan_id})
        - callback_secret: Secret token for authenticating uploads
    
    Output: Metadata about processed organs (files are uploaded directly to callback_url)
    """
    try:
        input_data = event.get("input", {})
        
        # Get file URL
        file_url = input_data.get("file_url")
        organs = input_data.get("organs", ["liver", "heart", "lungs"])  # Default organs
        fast_mode = input_data.get("fast", False)  # Default to full resolution for better GPU utilization
        
        # Callback for uploading results
        callback_url = input_data.get("callback_url")  # e.g., https://api.ar4ct.com/scans/{scan_id}
        
        if not file_url:
            return {"error": "No file_url provided"}
        
        # Create temp directory
        with tempfile.TemporaryDirectory() as tmp_dir:
            # Download file from URL
            print(f"Downloading file from URL: {file_url}")
            # Explicitly exclude brotli encoding (br) as requests doesn't support it natively
            headers = {"Accept-Encoding": "gzip, deflate"}
            response = requests.get(file_url, stream=True, timeout=300, headers=headers)
            response.raise_for_status()
            file_bytes = response.content
            print(f"Downloaded {len(file_bytes) / (1024*1024):.2f} MB")
            
            # Detect file type and extract if needed
            if file_bytes[:4] == b'PK\x03\x04':  # ZIP file magic bytes
                zip_path = os.path.join(tmp_dir, "input.zip")
                with open(zip_path, "wb") as f:
                    f.write(file_bytes)
                
                # Extract zip
                with zipfile.ZipFile(zip_path, 'r') as zip_ref:
                    zip_ref.extractall(tmp_dir)
                
                # Find the input file (MHD, NIfTI, or NRRD)
                mhd_files = glob.glob(os.path.join(tmp_dir, "**/*.mhd"), recursive=True)
                nifti_files = glob.glob(os.path.join(tmp_dir, "**/*.nii*"), recursive=True)
                nrrd_files = glob.glob(os.path.join(tmp_dir, "**/*.nrrd"), recursive=True)
                
                if mhd_files:
                    # Convert MHD to NIfTI (TotalSegmentator doesn't support MHD directly)
                    mhd_path = mhd_files[0]
                    print(f"Found MHD file: {mhd_path}, converting to NIfTI...")
                    img = sitk.ReadImage(mhd_path)
                    input_path = os.path.join(tmp_dir, "input_converted.nii.gz")
                    sitk.WriteImage(img, input_path)
                    print(f"Converted MHD to NIfTI: {input_path}")
                elif nifti_files:
                    input_path = nifti_files[0]
                elif nrrd_files:
                    # Convert NRRD to NIfTI as well for safety
                    nrrd_path = nrrd_files[0]
                    print(f"Found NRRD file: {nrrd_path}, converting to NIfTI...")
                    img = sitk.ReadImage(nrrd_path)
                    input_path = os.path.join(tmp_dir, "input_converted.nii.gz")
                    sitk.WriteImage(img, input_path)
                    print(f"Converted NRRD to NIfTI: {input_path}")
                else:
                    return {"error": "No supported file found in zip (mhd, nii, nii.gz, nrrd)"}
            else:
                # Assume NIfTI file
                input_path = os.path.join(tmp_dir, "input.nii.gz")
                with open(input_path, "wb") as f:
                    f.write(file_bytes)
            
            # Output directory - create it explicitly
            output_dir = os.path.join(tmp_dir, "segmentations")
            os.makedirs(output_dir, exist_ok=True)
            
            print(f"Input path: {input_path}")
            print(f"Output dir: {output_dir}")
            print(f"Input exists: {os.path.exists(input_path)}")
            print(f"Input is file: {os.path.isfile(input_path)}")
            print(f"Output dir exists: {os.path.exists(output_dir)}")
            print(f"Output dir is dir: {os.path.isdir(output_dir)}")
            
            # List tmp_dir contents for debugging
            print(f"Contents of tmp_dir ({tmp_dir}):")
            for root, dirs, files in os.walk(tmp_dir):
                level = root.replace(tmp_dir, '').count(os.sep)
                indent = ' ' * 2 * level
                print(f'{indent}{os.path.basename(root)}/')
                subindent = ' ' * 2 * (level + 1)
                for file in files:
                    filepath = os.path.join(root, file)
                    size = os.path.getsize(filepath) if os.path.isfile(filepath) else 0
                    print(f'{subindent}{file} ({size} bytes)')
            
            # Determine device - check if CUDA is actually available and working
            import torch
            print(f"PyTorch version: {torch.__version__}")
            print(f"CUDA available: {torch.cuda.is_available()}")
            
            if torch.cuda.is_available():
                device = "gpu"
                print(f"CUDA device count: {torch.cuda.device_count()}")
                print(f"CUDA current device: {torch.cuda.current_device()}")
                print(f"GPU name: {torch.cuda.get_device_name(0)}")
                print(f"CUDA version: {torch.version.cuda}")
                
                # Test CUDA with a simple operation
                try:
                    test_tensor = torch.zeros(1).cuda()
                    print(f"✓ CUDA test passed - tensor on device: {test_tensor.device}")
                    del test_tensor
                    torch.cuda.empty_cache()
                except Exception as e:
                    print(f"✗ CUDA test FAILED: {e}")
                    print("Falling back to CPU")
                    device = "cpu"
            else:
                device = "cpu"
                print("WARNING: CUDA not available, falling back to CPU (this will be slow!)")
            
            # Run TotalSegmentator with verbose output
            print(f"Starting TotalSegmentator with device={device}, fast={fast_mode}")
            print(f"  Input: {input_path}")
            print(f"  Output: {output_dir}")
            
            # Monitor GPU during inference
            if torch.cuda.is_available():
                print(f"  GPU memory before: {torch.cuda.memory_allocated() / 1024**2:.1f} MB")
            
            # Run in subprocess with timeout to prevent hanging
            success, message = run_totalsegmentator_subprocess(
                input_path=input_path,
                output_dir=output_dir,
                fast=fast_mode,
                device=device,
                timeout=7200  # 2 hours – large CTs with all 117 organs need ~60-90 min
            )
            
            if not success:
                return {"error": f"TotalSegmentator failed: {message}"}
            
            if torch.cuda.is_available():
                print(f"  GPU memory after: {torch.cuda.memory_allocated() / 1024**2:.1f} MB")
            
            print("TotalSegmentator completed successfully")
            
            # ── Extract body surface from the original CT scan ──
            if "body" in organs:
                body_stl_path = os.path.join(tmp_dir, "body.stl")
                try:
                    if extract_body_surface(input_path, body_stl_path):
                        print("  Body surface extraction succeeded")
                    else:
                        print("  Body surface extraction returned empty mask")
                except Exception as e:
                    print(f"  Body surface extraction failed: {e}")
                    import traceback
                    traceback.print_exc()

            # Convert requested organs to STL and upload/collect results
            results = {}
            uploaded_organs = []
            failed_organs = []
            
            for organ in organs:
                # Body surface is generated from the CT directly, not from TotalSegmentator
                if organ == "body":
                    stl_path = os.path.join(tmp_dir, "body.stl")
                    if os.path.exists(stl_path) and os.path.getsize(stl_path) > 84:
                        stl_size = os.path.getsize(stl_path)
                        print(f"  Using body surface STL: {stl_size / 1024:.1f} KB")
                        if callback_url:
                            try:
                                upload_url = f"{callback_url}/stl/body"
                                print(f"  Uploading body.stl to {upload_url}")
                                with open(stl_path, "rb") as f:
                                    upload_response = requests.post(
                                        upload_url,
                                        files={"file": ("body.stl", f, "model/stl")},
                                        timeout=300
                                    )
                                if upload_response.status_code == 200:
                                    print("  \u2713 Uploaded body.stl successfully")
                                    uploaded_organs.append("body")
                                    results["body"] = {"status": "uploaded", "size": stl_size}
                                else:
                                    print(f"  \u2717 Failed to upload body.stl: {upload_response.status_code}")
                                    failed_organs.append("body")
                                    results["body"] = {"status": "upload_failed", "error": upload_response.text}
                            except Exception as e:
                                print(f"  \u2717 Error uploading body.stl: {e}")
                                failed_organs.append("body")
                                results["body"] = {"status": "upload_error", "error": str(e)}
                        else:
                            with open(stl_path, "rb") as f:
                                results["body"] = base64.b64encode(f.read()).decode("utf-8")
                            uploaded_organs.append("body")
                    else:
                        print("  Body surface STL not available")
                        failed_organs.append("body")
                    continue

                nifti_path = os.path.join(output_dir, f"{organ}.nii.gz")
                if os.path.exists(nifti_path):
                    stl_path = os.path.join(tmp_dir, f"{organ}.stl")
                    if nifti_to_stl(nifti_path, stl_path):
                        stl_size = os.path.getsize(stl_path)
                        print(f"  Generated STL for {organ}: {stl_size / 1024:.1f} KB")
                        
                        # If callback URL is provided, upload the STL file
                        if callback_url:
                            try:
                                upload_url = f"{callback_url}/stl/{organ}"
                                print(f"  Uploading {organ}.stl to {upload_url}")
                                
                                with open(stl_path, "rb") as f:
                                    upload_response = requests.post(
                                        upload_url,
                                        files={"file": (f"{organ}.stl", f, "model/stl")},
                                        timeout=300
                                    )
                                
                                if upload_response.status_code == 200:
                                    print(f"  ✓ Uploaded {organ}.stl successfully")
                                    uploaded_organs.append(organ)
                                    results[organ] = {
                                        "status": "uploaded",
                                        "size": stl_size,
                                        "url": f"{callback_url}/stl/{organ}"
                                    }
                                else:
                                    print(f"  ✗ Failed to upload {organ}.stl: {upload_response.status_code} - {upload_response.text}")
                                    failed_organs.append(organ)
                                    results[organ] = {
                                        "status": "upload_failed",
                                        "error": upload_response.text
                                    }
                            except Exception as e:
                                print(f"  ✗ Error uploading {organ}.stl: {e}")
                                failed_organs.append(organ)
                                results[organ] = {
                                    "status": "upload_error",
                                    "error": str(e)
                                }
                        else:
                            # No callback URL - return base64 encoded (may fail if too large)
                            print(f"  Warning: No callback_url provided, returning base64 (may exceed size limit)")
                            with open(stl_path, "rb") as f:
                                results[organ] = base64.b64encode(f.read()).decode("utf-8")
                            uploaded_organs.append(organ)
                else:
                    print(f"  Organ {organ} not found in segmentation output")
            
            print(f"\nProcessed {len(uploaded_organs)} organs: {uploaded_organs}")
            if failed_organs:
                print(f"Failed to upload {len(failed_organs)} organs: {failed_organs}")
            
            return {
                "status": "success",
                "organs_processed": uploaded_organs,
                "organs_failed": failed_organs,
                "results": results,
                "callback_url": callback_url
            }
            
    except Exception as e:
        return {"error": str(e)}

# Start the serverless worker
runpod.serverless.start({"handler": handler})
