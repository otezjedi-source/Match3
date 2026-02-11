Shader "Match3/Bubble"
{
    Properties
    {
        [Header(Color Animation)]
        _ColorSpeed ("Color Speed", Float) = 1.0
        _IridescenceIntensity ("Iridescence Intensity", Range(0, 3)) = 1.5
        _RotationSpeed ("Rotation Speed", Range(0, 2)) = 0.3
        
        [Header(Visual Effects)]
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.8
        _RimPulseSpeed ("Rim Pulse Speed", Range(0, 5)) = 2.0
        _SpecularPower ("Specular Power", Range(8, 128)) = 32.0
        _SpecularIntensity ("Specular Intensity", Range(0, 5)) = 2.0
        
        [Header(Transparency)]
        _AlphaMultiplier ("Alpha Edge Multiplier", Range(0, 3)) = 1.2
        _AlphaCenter ("Alpha Center", Range(0, 1)) = 0.2
        
        [Header(Size Variation)]
        _SizeMin ("Min Size", Range(0.5, 1)) = 0.7
        _SizeMax ("Max Size", Range(1, 2)) = 1.3
        
        [Header(Wobble Animation)]
        _WobbleAmount ("Wobble Amount", Range(0, 0.2)) = 0.05
        _WobbleSpeed ("Wobble Speed", Range(0, 5)) = 2.0
        
        [Header(Pop Animation)]
        _PopStartThreshold ("Pop Start", Range(0, 1)) = 0.8
        _PopBrightness ("Pop Brightness", Range(0, 5)) = 3.0
        _CrackIntensity ("Crack Intensity", Range(0, 1)) = 0.6
        _CrackFrequency ("Crack Frequency", Float) = 25.0
        _CrackAnimSpeed ("Crack Animation Speed", Float) = 12.0
        
        [Header(Lighting)]
        _LightDirection ("Light Direction", Vector) = (0.5, 1, 0.5, 0)
    }
    
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        
        Pass
        {
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
        
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // Math constants
            static const float RGB_PHASE_SHIFT = 2.09439510239; // 2PI/3 (120 degrees)
            
            // Shape constants
            static const float RADIUS_SQ = 0.25;        // Square radius (0.5^2)
            static const float MASK_SHARPNESS = 5.0;    // Edge sharpness
            static const float EDGE_FACTOR = 4.0;       // Edge detection multiplier
            
            // Crack constants
            static const float CRACK_FREQUENCY = 30.0;
            static const float CRACK_ANIM_SPEED = 15.0;
            static const float CRACK_SCALE = 0.25;
            static const float CRACK_OFFSET = 0.5;
            
            CBUFFER_START(UnityPerMaterial)
                half _ColorSpeed;
                half _IridescenceIntensity;
                half _RotationSpeed;
                half _RimIntensity;
                half _RimPulseSpeed;
                half _SpecularPower;
                half _SpecularIntensity;
                half _AlphaMultiplier;
                half _AlphaCenter;
                half _SizeMin;
                half _SizeMax;
                half _WobbleAmount;
                half _WobbleSpeed;
                half _PopStartThreshold;
                half _PopBrightness;
                half _CrackIntensity;
                half _CrackFrequency;
                half _CrackAnimSpeed;
                half3 _LightDirection;
            CBUFFER_END
            
            // Instance data:
            // x = normalized age (0-1, 0=just spawned, 1=about to die)
            // y = color seed (0-1, random per bubble for color variation)
            // z = size variation (0-1, used with _SizeMin/_SizeMax for random sizes)
            uniform float3 _BubbleInfoArray[1023];

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half2 uv : TEXCOORD0;
                half2 data : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            ///////////////////////////////////////////////////////////////
            // Helpers
            ///////////////////////////////////////////////////////////////
            // Manhattan distance for diagonal iridescence pattern
            half ManhattanDistance(half2 v)
            {
                return (abs(v.x) + abs(v.y)) * INV_SQRT2;
            }
            
            // Rotate UV coordinates by angle (radians)
            half2 RotateUV(half2 uv, half angle)
            {
                half s = sin(angle);
                half c = cos(angle);
                return half2(
                    uv.x * c - uv.y * s,
                    uv.x * s + uv.y * c
                );
            }
            
            // Convert phase to RGB with 120° shifts for rainbow effect
            half3 PhaseToRGB(half phase)
            {
                return half3(
                    sin(phase) * 0.5 + 0.5,
                    sin(phase + RGB_PHASE_SHIFT) * 0.5 + 0.5,
                    sin(phase + RGB_PHASE_SHIFT * 2.0) * 0.5 + 0.5
                );
            }
            
            // Calculate pulsing animation value (0 to 1)
            half CalculatePulse(half time, half seed, half speed)
            {
                return sin(time * speed + seed * TWO_PI) * 0.5 + 0.5;
            }
            
            ///////////////////////////////////////////////////////////////
            // Shape calculations
            ///////////////////////////////////////////////////////////////
            // Circular mask: 1.0 at center, 0.0 outside
            half CalculateCircleMask(half2 centeredUV)
            {
                half distSq = dot(centeredUV, centeredUV);
                return saturate((RADIUS_SQ - distSq) * MASK_SHARPNESS);
            }
            
            // Edge detection: 0.0 at center, 1.0 at edges
            half CalculateEdgeFactor(half2 centeredUV)
            {
                half distSq = dot(centeredUV, centeredUV);
                return saturate(distSq * EDGE_FACTOR);
            }
            
            // Simplified Fresnel effect (squared edge for smoother falloff)
            half CalculateFresnel(half edge)
            {
                return edge * edge;
            }
            
            // Calculate approximate sphere normal for lighting
            // Uses sphere equation: x² + y² + z² = r²
            half3 CalculateSphereNormal(half2 uv)
            {
                half distSq = dot(uv, uv);
                
                // Early out for pixels outside bubble
                if (distSq > RADIUS_SQ)
                    return half3(0, 0, 1);
                
                // Calculate z component from sphere equation
                half z = sqrt(RADIUS_SQ - distSq);
                
                return normalize(half3(uv, z));
            }
            
            ///////////////////////////////////////////////////////////////
            // Wobble animation
            ///////////////////////////////////////////////////////////////
            // Apply organic wobble deformation to UV
            // Creates "jelly" bubble effect with different frequencies for x/y
            half2 ApplyWobble(half2 uv, half time, half seed)
            {
                half wobblePhase = time * _WobbleSpeed + seed * TWO_PI;
                
                // Different frequencies for x and y to avoid symmetry
                half wobbleX = sin(wobblePhase * 1.3) * _WobbleAmount;
                half wobbleY = cos(wobblePhase * 1.7) * _WobbleAmount;
                
                // Stronger wobble at edges
                half distFromCenter = length(uv);
                half wobbleStrength = distFromCenter;
                
                return uv + half2(wobbleX, wobbleY) * wobbleStrength;
            }
            
            ///////////////////////////////////////////////////////////////
            // Color calculations
            ///////////////////////////////////////////////////////////////
            // Iridescent rainbow colors with rotation animation
            // Uses Manhattan distance for diagonal patterns
            half3 CalculateIridescence(half2 centeredUV, half time, half colorSeed, half rotation)
            {
                // Rotate UV for spinning rainbow effect
                half2 rotatedUV = RotateUV(centeredUV, rotation);
                
                half dist = ManhattanDistance(rotatedUV);
                half phase = dist * _IridescenceIntensity * TWO_PI + time + colorSeed * TWO_PI;
                
                return PhaseToRGB(phase);
            }
            
            // Rim lighting with pulsing animation
            // Uses cubic falloff for sharp edge highlight
            half3 CalculateRimLight(half edge, half pulse)
            {
                // Cubic falloff for sharp edge highlight
                half rimStrength = edge * edge * edge;
                
                // Modulate intensity with pulse (70% base + 30% pulse)
                half intensity = _RimIntensity * (0.7 + pulse * 0.3);
                
                return half3(1, 1, 1) * rimStrength * intensity;
            }
            
            // Specular highlight using Blinn-Phong model
            // Creates bright reflection spots like real soap bubbles
            half3 CalculateSpecular(half3 normal, half3 lightDir)
            {
                // View direction (camera looking straight at bubble)
                half3 viewDir = half3(0, 0, 1);
                
                // Half vector for Blinn-Phong
                half3 halfDir = normalize(lightDir + viewDir);
                
                // Specular calculation
                half NdotH = max(0, dot(normal, halfDir));
                half spec = pow(NdotH, _SpecularPower);
                
                return half3(1, 1, 1) * spec * _SpecularIntensity;
            }
            
            ///////////////////////////////////////////////////////////////
            // Alpha calculations
            ///////////////////////////////////////////////////////////////
            // Calculate base alpha with realistic bubble transparency gradient
            // Center is more transparent, edges more opaque (like real bubbles)
            half CalculateBaseAlpha(half circleMask, half fresnel, half edge)
            {
                // Lerp from center alpha to edge alpha
                half alpha = lerp(_AlphaCenter, fresnel * _AlphaMultiplier, edge);
                return alpha * circleMask;
            }
            
            ///////////////////////////////////////////////////////////////
            // Pop animation
            ///////////////////////////////////////////////////////////////
            // Calculate pop animation progress
            // Returns 0 when not popping, 1 when fully popped
            half CalculatePopProgress(half normalizedAge)
            {
                half progress = (normalizedAge - _PopStartThreshold) / (1.0 - _PopStartThreshold);
                return saturate(progress) * step(_PopStartThreshold, normalizedAge);
            }
            
            // Generate animated crack pattern for bubble pop effect
            // Creates cross-hatched diagonal lines that move over time
            half CalculateCrackPattern(half2 centeredUV, half normalizedAge)
            {
                half2 absUV = abs(centeredUV);
                
                // Two sets of diagonal cracks moving at different speeds
                half diagonal1 = (absUV.x + absUV.y) * _CrackFrequency + normalizedAge * _CrackAnimSpeed;
                half diagonal2 = (absUV.x - absUV.y) * CRACK_FREQUENCY + normalizedAge * CRACK_ANIM_SPEED;
                
                // Combine both patterns with sine waves
                half pattern = sin(diagonal1) + sin(diagonal2);
                
                // Normalize to 0-1 range
                return pattern * CRACK_SCALE + CRACK_OFFSET;
            }
            
            // Vertex shader
            Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;
                
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                
                // Read instance data
                float3 info = _BubbleInfoArray[instanceID];
                half size = info.z; // Size variation (0-1) for this bubble instance
                
                // Apply size variation
                half sizeScale = lerp(_SizeMin, _SizeMax, size);
                float3 scaledPosition = IN.positionOS.xyz * sizeScale;
                
                // Transform to clip space
                OUT.positionCS = TransformObjectToHClip(scaledPosition);
                
                // Center UV coordinates (-0.5 to 0.5 for centered calculations)
                OUT.uv = IN.uv - 0.5h;
                
                // Pass instance data to fragment shader
                OUT.data = _BubbleInfoArray[instanceID].xy;
                
                return OUT;
            }

            // Fragment shader
            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                
                // Extract bubble data
                half normalizedAge = IN.data.x;
                half colorSeed = IN.data.y;
                half time = _Time.y * _ColorSpeed;
                
                // Apply wobble deformation to create organic movement
                half2 wobbledUV = ApplyWobble(IN.uv, time, colorSeed);
                
                // Calculate bubble shape
                half mask = CalculateCircleMask(wobbledUV);
                half edge = CalculateEdgeFactor(wobbledUV);
                half fresnel = CalculateFresnel(edge);
                
                // Calculate rotation for spinning rainbow pattern
                half rotation = time * _RotationSpeed + colorSeed * TWO_PI;
                
                // Base iridescent colors
                half3 iridescence = CalculateIridescence(wobbledUV, time, colorSeed, rotation);
                
                // Pulsing rim light
                half rimPulse = CalculatePulse(time, colorSeed, _RimPulseSpeed);
                half3 rimLight = CalculateRimLight(edge, rimPulse);
                
                // Specular highlights
                half3 normal = CalculateSphereNormal(wobbledUV);
                half3 lightDir = normalize(_LightDirection);
                half3 specular = CalculateSpecular(normal, lightDir);
                
                // Combine all lighting components
                half3 color = iridescence + rimLight + specular;
                
                // Calculate transparency with gradient
                half alpha = CalculateBaseAlpha(mask, fresnel, edge);
                
                // Apply pop animation effects
                half popProgress = CalculatePopProgress(normalizedAge);
                
                // Bright flash when popping
                color += popProgress * _PopBrightness;
                color = saturate(color);
                
                // Fade out during pop
                alpha *= 1.0 - popProgress;
                
                // Apply animated crack pattern
                if (popProgress > 0.0)
                {
                    half crackPattern = CalculateCrackPattern(wobbledUV, normalizedAge);
                    half crackMultiplier = lerp(1.0, crackPattern, popProgress * _CrackIntensity);
                    alpha *= crackMultiplier;
                }
                
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
