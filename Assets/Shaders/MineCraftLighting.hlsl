#ifndef MINECRAFT_LIGHTING_INCLUDED
#define MINECRAFT_LIGHTING_INCLUDED

float _MineCraftSkyLight;

half4 MineCraftResolveVertexColor(half4 color)
{
    if (color.r + color.g + color.b < 0.01h)
    {
        return half4(1.0h, 1.0h, 1.0h, max(color.a, 1.0h));
    }

    return color;
}

half MineCraftResolveSkyLight()
{
    return _MineCraftSkyLight > 0.001h ? _MineCraftSkyLight : 1.0h;
}

#endif
