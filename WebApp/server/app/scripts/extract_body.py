"""
Extract the outer body surface from a CT NIfTI scan and save as STL.

Usage:
    python extract_body.py --ct /path/to/ct.nii.gz --output /path/to/body.stl

Uses a Hounsfield-Unit threshold to separate body from air, then
applies binary morphological closing to fill small holes before
running marching cubes.
"""

import argparse
import sys
import zipfile
import tempfile
from pathlib import Path

import nibabel as nib
import SimpleITK as sitk
import numpy as np
from scipy import ndimage
from skimage import measure
from stl import mesh as stl_mesh


def load_image_volume(path: str, hu_threshold: float) -> tuple[np.ndarray, tuple[float, float, float]]:
    """
    Load a 3D image volume and return a binary BODY MASK directly to save memory.
    Returns (mask_uint8, spacing).
    """
    p = Path(path)
    
    # Try SimpleITK first for everything as it's more memory efficient for thresholding
    try:
        reader = sitk.ImageFileReader()
        reader.SetFileName(path)
        img = reader.Execute()
        
        # Calculate spacing (x, y, z)
        spacing = img.GetSpacing()
        
        # Cast to Float32 so thresholding works for unsigned pixel types (e.g. MHD)
        img = sitk.Cast(img, sitk.sitkFloat32)
        
        # Threshold in SimpleITK (returns uint8 image 0/1)
        # We want > hu_threshold. So (hu_threshold, infinity).
        # Note: SimpleITK uses [lower, upper].
        mask_img = sitk.BinaryThreshold(img, lowerThreshold=hu_threshold, upperThreshold=100000.0, insideValue=1, outsideValue=0)
        
        # Free original image memory
        del img
        
        # Convert to numpy
        mask = sitk.GetArrayFromImage(mask_img) # (z, y, x)
        mask = np.transpose(mask, (2, 1, 0))    # (x, y, z) to match marching cubes
        
        return mask, spacing
        
    except Exception as e:
        print(f"  SimpleITK load failed ({e}), trying nibabel fallback...")
    
    # Fallback to nibabel (mainly for .nii.gz if SimpleITK failed or wasn't used)
    img = nib.load(path)
    data = img.get_fdata() # This uses lot of memory for float64
    spacing = img.header.get_zooms()[:3]
    
    mask = (data > hu_threshold).astype(np.uint8)
    del data # Free float array
    return mask, spacing


def extract_body_surface(
    input_nifti_path: str,
    stl_path: str,
    hu_threshold: float = -500.0,
) -> str | None:
    """
    Extract the outer body surface from a CT scan and save as STL.

    Args:
        input_nifti_path: Path to the original CT NIfTI file (or .zip containing one).
        stl_path:         Where to write the resulting STL.
        hu_threshold:     HU value above which voxels are considered "body".
                          -500 captures skin, fat, muscle, bone, etc.

    Returns:
        stl_path on success, None if the mask is empty.
    """
    input_path = Path(input_nifti_path)
    temp_dir = None

    try:
        if input_path.suffix.lower() == ".zip":
            print(f"  Unzipping {input_path} ...")
            temp_dir = tempfile.TemporaryDirectory()
            with zipfile.ZipFile(input_path, "r") as z:
                z.extractall(temp_dir.name)
            
            # Find the first .nii, .nii.gz, .mhd, or .nrrd file
            image_files = list(Path(temp_dir.name).rglob("*"))
            candidates = [
                f for f in image_files 
                if f.suffix.lower() in (".nii", ".gz", ".mhd", ".nrrd")
            ]
            
            if not candidates:
                print("  ERROR: No supported image file (nii/gz/mhd/nrrd) in zip")
                return None
            
            # Prefer .nii.gz / .nii if multiple
            niftis = [f for f in candidates if ".nii" in f.name.lower()]
            load_path = str(niftis[0]) if niftis else str(candidates[0])
            print(f"  Found image file: {load_path}")
        else:
            load_path = str(input_path)

        print(f"  Extracting body surface (HU > {hu_threshold}) from {load_path} ...")
        
        try:
            # Load DIRECTLY as binary mask to save RAM
            body_mask, spacing = load_image_volume(load_path, hu_threshold)
        except Exception as e:
            print(f"  ERROR: Failed to load image: {e}")
            return None

        if body_mask.max() == 0:
            print("  SKIP: body mask is empty")
            return None
    finally:
        if temp_dir:
            temp_dir.cleanup()


    # Morphological closing to fill internal gaps and smooth the surface
    struct = ndimage.generate_binary_structure(3, 2)
    body_mask = ndimage.binary_closing(
        body_mask, structure=struct, iterations=3
    ).astype(np.uint8)

    # Keep only the largest connected component (the body)
    labelled, num_features = ndimage.label(body_mask)
    if num_features > 1:
        component_sizes = ndimage.sum(body_mask, labelled, range(1, num_features + 1))
        largest = int(np.argmax(component_sizes)) + 1
        body_mask = (labelled == largest).astype(np.uint8)

    # Subsample for performance if the volume is very large
    step = 1
    total_voxels = int(body_mask.sum())
    if total_voxels > 20_000_000:
        step = 2  # halve resolution
        print(f"  Body mask has {total_voxels} voxels – using step_size={step}")

    verts, faces, _, _ = measure.marching_cubes(body_mask, level=0.5, step_size=step)

    # Transform voxel coords → physical coords (mm)
    # marching_cubes with step_size already returns coords in original voxel space
    spacing = np.array(spacing)
    verts = verts * spacing

    # Build STL mesh
    stl_obj = stl_mesh.Mesh(np.zeros(faces.shape[0], dtype=stl_mesh.Mesh.dtype))
    for i, face in enumerate(faces):
        for j in range(3):
            stl_obj.vectors[i][j] = verts[face[j], :]

    Path(stl_path).parent.mkdir(parents=True, exist_ok=True)
    stl_obj.save(stl_path)

    import os

    size_kb = os.path.getsize(stl_path) / 1024
    print(f"  Body surface STL: {size_kb:.1f} KB ({len(faces)} faces)")
    return stl_path


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Extract body surface from CT NIfTI → STL"
    )
    parser.add_argument("--ct", required=True, help="Path to CT NIfTI file (.nii.gz)")
    parser.add_argument("--output", required=True, help="Output STL path")
    parser.add_argument(
        "--hu-threshold",
        type=float,
        default=-500.0,
        help="HU threshold for body/air separation (default: -500)",
    )
    args = parser.parse_args()

    ct_path = Path(args.ct)
    if not ct_path.exists():
        print(f"ERROR: CT file not found: {ct_path}")
        sys.exit(1)

    result = extract_body_surface(str(ct_path), args.output, args.hu_threshold)
    if result is None:
        print("ERROR: Body surface extraction returned empty mask")
        sys.exit(1)

    print("Done.")


if __name__ == "__main__":
    main()
