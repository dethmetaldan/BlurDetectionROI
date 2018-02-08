# BlurDetectionROI
This Windows Forms application gives the client recommendations for images based on the calculated sharpness value. Users can open an image and select a region of interest within the image to examine. The program will compute the sharpness based on this region and output both the sharpness value and "Blurry" or "Not Blurry" to the user. The window will zoom in to the image's region of interest to enable users to determine if the algorithm has given them a false negative. The user can also switch between seeing the edges in the image or the full RGB image.

## Algorithm Description
This blur detection algorithm works by convolving the grayscale channel of an image with a 3x3 kernel, specifically the Laplacian kernel. The Laplacian operator is used to measure the 2nd derivative of an image and is commonly used for edge detection. It highlights rapid changes in the gray levels of an image. To determine the sharpness of an image, we look at the variance of the laplacian operator. If there is a high variance, then there are many edges and non-edges, which is typically representative of an image that is in-focus. Likewise, a low variance tells us there are very few edges within the image, indicative of a blurry image. To increase the accuracy of the variance of the laplacian operator, we need to do some image pre-processing.

### Contrast Limited Adaptive Histogram Equalization
Adaptive Histogram Equalization (AHE) is a method of distributing the lightness values of an image, thereby increasing local contrast. If the dataset includes images that contain regions that are significantly darker or lighter than the rest of the image, it can affect the laplacian operator's edge detection. Evening out the histogram just slightly can help with these cases. Contrast Limited Adaptive Histogram Equalization (CLAHE) is similar to AHE, with the difference being CLAHE prevents the overamplification of noise that AHE can produce.

### Bilateral Filter
Since the laplacian operator is a 2nd derivative operator, it is highly sensitive to noise. Typically, the first course of action would be to apply a gaussian blur over the image. Since the images in my datasets are taken with very high ISOs and thereby quite noisy, gaussian blur is not sufficient. An extremely strong gaussian blur was needed to remove the noise, but in the process also blurred all the edges, rendering the laplacian inaccurate. While the bilateral filter is typically slower than a gaussian filter, it handles edges much better. The bilateral filter blurs the smoother, more uniform areas of an image while preserving the edges. 

## Use Cases
There are three primary use cases for this program. The first is for noisy, high ISO images. As mentioned previously, I have found that many of the common blur detection implementations use processing steps that are more suited for low noise images. The second is for images with a strong bokeh, as the laplacian operator cannot tell the difference between blur that is desirable, as in the case of bokeh, and and undesirable. Finally, if the user is only interested in the sharpness of a portion of the image, it is useful to be able to select the region of interest rather than computing an overall blur metric.

## Built With
- .NET Framework 4.6.1
- Emgu CV
