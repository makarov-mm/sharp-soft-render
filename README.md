Software render on C# (WinForms, GDI+)

[![GitHub Sponsors](https://img.shields.io/github/sponsors/makarov-mm?style=flat&logo=github)](https://github.com/sponsors/makarov-mm)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
![C#](https://img.shields.io/badge/C%23-239120)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/makarov-mm/)
[![Threads](https://img.shields.io/badge/Threads-000000?logo=threads&logoColor=white)](https://www.threads.net/@m.m.makarov)
[![Instagram](https://img.shields.io/badge/Instagram-E4405F?logo=instagram&logoColor=white)](https://www.instagram.com/m.m.makarov/)
![Windows](https://img.shields.io/badge/Windows-0078D6?logo=windows&logoColor=white)

Features:
- Barycentric triangle rasterizer with a Z-buffer and per-pixel interpolation of depth, UV and normals
- Texture mapping: bilinear filtering, repeat wrapping (PNG/JPG/BMP via GDI+)
- Per-pixel lighting: Lambert diffuse + Blinn-Phong specular + ambient, smooth (Phong) shading for models with vertex normals
- Wavefront OBJ/MTL loader (v/vt/vn, negative indices, n-gon fan triangulation, map_Kd); smooth vertex normals are computed automatically when the file has none
- DXF loader (MESH, 3DFACE)
- Sample textured models included: crate cube, planet sphere, checkered torus

Please, visit my page on [https://www.linkedin.com/in/makarov-mm/](https://www.linkedin.com/in/makarov-mm/)

![Soft Renderer](Screenshots/screenshot1.png)

![Soft Renderer](Screenshots/screenshot3.png)

## Support

If you found this project interesting or useful, you can support my work:

[![GitHub Sponsors](https://img.shields.io/github/sponsors/makarov-mm?style=flat&logo=github)](https://github.com/sponsors/makarov-mm)