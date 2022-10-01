# SoftRasterizer
`SoftRasterizer` is a project using cs script to replace integrated rasterizer of unity engine.

It is based on the knowledge and framework of Games101 by Prof. Lingqi Yan.

Here I reconstruct the c++ code into unity engine to get away from problems inputing models and setup scenes.

## Framework
The `SoftRasterizer` is design to render scene to screen avoid using Graphic API. 

It use a monobehavior script "CameraRenderer" attached to camera to collect scene information. It will collect main camera and main light, and also all object marked with script [`RenderingObject`](./Assets/SoftRasterizer/Runtime/RenderingObject.cs).

After collect all information needed, it will pass them to script [`Rasterizer`](./Assets/SoftRasterizer/Runtime/Rasterizer.cs) to produce a texture of screen size. 

The output texture is bound to a `rawImg` of size screen and is shown directly to camera.

### **[Camera Renderer](./Assets/SoftRasterizer/Runtime/CameraRenderer.cs)**
Collect scene infomation (camera, light and rendering objects) in function `Start()`, using `OnPostRender()` function to pass them to rasterizer, so that every time camera need to render a frame, it calls script `Rasterizer` to render.
``` csharp
private void OnPostRender()
{
    rasterizer.Render(_camera, _light, _renderingObjects);
}
```

### **[Rasterizer](./Assets/SoftRasterizer/Runtime/Rasterizer.cs)**
The core function of `Rasterizer` is its rendeing pipeline.
```csharp
public void Render(Camera camera, Light light, List<RenderingObject> renderingObjects)
{
    Clear();
    SetupInfo(camera, light);
    ShadowCastingPass(camera,light);
    Draw(renderingObjects);
    Submit();
}
```

## Pipeline
The pipeline goes as clear, setup, shadow cast, draw and submit.

<span style="color:grey">NOTE: Because we are avoiding using graphic APIs, we have to manually handle various procedure that is supposed to be handled by hardware of engine itself, this will result difference between modern graphic pipelines.</span>

- Clear
  - Everytime a render call is posted from 
- Setup
  - Get camera and light information and setup View and Projection matrices based on it
- Shadow Cast
  - Collect main camera and move it to light posision and record shadow map to _bufShadow for later check during draw call
- [Draw](#draw)
  - Object is drawn to buffer inside draw method.
- Submit
  - Apply buffer to texture binded to a rawImg of screen size.

### Draw
- Frustum culling
  - If a object is outside frustum, skip and not render it.
- Vertex shader
  - If the object pass the culling, it will be sent into vertex shader.
  - Vertex buffer is set during vertex shader, which contains all verteces need to be rendered.
- Primitive Assembly
  - Matches triangle indecies with verteces.
- Clipping
  - Check if a triangle is out side frustum, if outside, skip this triangle
- Back-Culling
  - Check triangle's normal, if it is looking the same z-coord direction, consider it on the back of a surface and we can not see it, skip this triangle.
- Viewport Transform
  - Transform NDC [0,1],[0,1] to screen coordinate [0,w],[0,h]
- Rasterize
  - Setup triangles information and call rasterizer.

### Rasterizer
- Setup 
  - Set up verteces information of input triangle.
- Get Bounding Box
  - Get bounding box of input triangle.
- Traverse Bounding Box
  - Traverse through the bounding box area and check each pixel.
- For each Pixel
  - Calculate barycentric coordinates
  - Check if pixel is inside triagle 
    - Check if pixel is inside triagle  using barycentric coordinate (if any of them < 0), skip those pixels not inside. Note that GAMES 101 code use cross product to check, which can be slower.
  - Perspective Correct Interpolation
    - Point in triangle on screen space will not remain its scale to triangle in world space. 
    
        This is because we did perspective transformation, and notice that value z is not linearly mapped to screen space. 
        
        Instead, 1/z is the one linearly mapped.

        So we need a correct interpolation to get correct z value in world space.

        *Check Fundamentals of computer Graphics, Fourth edition, Chapter 11, Texture mapping, P256.*
        <span style="color:yellow">Note that there might be a typo at formula (11.3), the coefficient of gamma should be 1 instead of 2 according to my test.</span>
    - Depth test
      - Test depth using correct z value and write to depth buffer if point is nearer, else skip this point.
    - Shadow test
      - Normally will be in fragment shader, but place it here will be faster because we only have one directional light and we can skip fragement shader if the pixel is in shadow.
      - Color pixel in shadow black.
    - Interpolate Verteces attributes
      - Use perspective correct interpolation to interpolate vertex attributes such as UV coord and etc.
    - Fragment shader
      - Use interpolated vertex information to do fragment shading.
      - Can do bilinear interpolation if `bool useBilinear` in payload set to true.
      - shade on color buffer.



## Current Progress
- Can correctly show the scene on rasterized output img
- Support texture
- Support Blinn-Phong and Unlit shader without Graphic API
- Shadow map is not finished, but place has been left in the pipeline.

## File Structures
### [CameraRenderer.cs](./Assets/SoftRasterizer/Runtime/CameraRenderer.cs) 
``` csharp
public class CameraRenderer : MonoBehaviour
```
- Script attached to main camera to collect scene.

### [FragmentShader.cs](./Assets/SoftRasterizer/Runtime/FragmentShader.cs) 
- Constains struct and classes used for fragment shading, unlit shader and Blinn-Phong shader is included.
``` csharp
public struct FragShaderPayload
```
- Shader payload contains verteces information.
``` csharp
public struct FragShaderWorldInfo
```
- Shader payload contains world camera and light information.
``` csharp
public class FragmentShader
```
- Shader class contains unlit and Blinn-Phong Shader.

### [ProfileManager.cs](./Assets/SoftRasterizer/Runtime/ProfileManager.cs)
- Wrapped up class for profile sampling.

### [Rasterizer.cs](./Assets/SoftRasterizer/Runtime/Rasterizer.cs)
```cs
public class Rasterizer
```
- INPORTANT file contains all rendering pipeline, rasterizer included

### [RenderingObject.cs](./Assets/SoftRasterizer/Runtime/RenderingObject.cs) 
```cs
public class RenderingObject : MonoBehaviour
```
- ATTACH THIS TO OBJECT SO IT CAN BE RENDERED!
- Collect object informations(mesh, etc).

### [Shadow.cs](./Assets/SoftRasterizer/Runtime/Shadow.cs)
```cs
public class Shadow
```
- Contains method to build shadow map.

### [ShowFPS.cs](./Assets/SoftRasterizer/Runtime/showFPS.cs)
- Binded to text to show frame rate.

### [Triangle.cs](./Assets/SoftRasterizer/Runtime/Triangle.cs)
```cs
public struct TVertex
```
- Structure of one triangle vertex data.

```cs
public class Triangle
```
- A triangle contains 3 `TVertex` in a array format `TVertex[]`.

### [Utility.cs](./Assets/SoftRasterizer/Runtime/Utility.cs)
```cs
public class Utility
```
- A class holding miscellaneous static functions, such as homogeneous coordinate convertion, view and projection Matrix setup and frustum culling.

### [VertexShader.cs](./Assets/SoftRasterizer/Runtime/VertexShader.cs)
```cs
public struct VertexBuff
```
- A structure recording vertex informations including clip-space position, world-space position, object (local) normal and world normal.
```cs
public struct VertexPayload
```
- Payload for vertex shading, contains rendering objects, view and projection matrix.
```cs
public class VertexShader
```
- Vertex shader setup vertex buffer `VertexBuff[]` using input payload information (rendering objects, view and projection matrix).


## Reference
 1. Lingqi, Yan. Games 101, 2020. 
 2. Shirley, P., Ashikhmin, M., & Marschner, S. (2009). Fundamentals of computer graphics. AK Peters/CRC Press.
 3. Akenine-Moller, T., Haines, E., & Hoffman, N. (2019). Real-time rendering. AK Peters/crc Press.
 4. Lengyel, E. (2019). Foundations of Game Engine Development: Rendering. Terathon Software LLC..
 5. Jasper Flick, Catlike Coding Tutorial, https://catlikecoding.com/unity/tutorials/rendering/.
 6. Joey de Vries, LearnOpenGL, https://learnopengl.com/Advanced-Lighting/Shadows/Shadow-Mapping
 7. 涟涟涟涟, CSDN blog, https://blog.csdn.net/weixin_43784914/article/details/123710140, 2022.
 8. n5, CSDN blog, https://blog.csdn.net/n5/article/details/123402012, 2022.
 9. AKGWSB, ToyRenderPipeline, https://github.com/AKGWSB/ToyRenderPipeline