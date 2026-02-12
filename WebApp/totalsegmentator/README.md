# TotalSegmentator on RunPod

To run [TotalSegmentator](https://github.com/wasserth/TotalSegmentator) in the cloud I use [RunPod.io](https://runpod.io/) for a cost-effective, serverless deployment.

## Docker Image

The Docker image used for deployment:
* [Docker Hub: simon1999/totalsegmentator-runpod](https://hub.docker.com/repository/docker/simon1999/totalsegmentator-runpod)

### Build and Push the Docker Image

This image can take several minutes to build
```bash
docker build --platform linux/amd64 -t simon1999/totalsegmentator-runpod:latest .
docker push simon1999/totalsegmentator-runpod:latest
```

## RunPod Setup

To deploy on RunPod, configure your container as shown below:
- GPU configuration: *16GB*
- Container Disk: *20GB*
- Container image: `simon1999/totalsegmentator-runpod:latest`

![RunPod Container Configuration](ConterinConfigRunpod.png)
Simons RunPod Dasbord [Overview](https://console.runpod.io/serverless/user/endpoint/z13vo1tf9l3sst)

---
For more details, see the [TotalSegmentator GitHub repository](https://github.com/wasserth/TotalSegmentator).