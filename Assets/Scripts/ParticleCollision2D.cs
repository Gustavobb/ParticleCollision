using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ParticleCollision2D : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private ComputeShader _shader;
    [SerializeField] private RawImage _image;

    private const float MAX_STEPS = 50f;
    private const float MIN_STEPS = 0f;
    [Range(MIN_STEPS, MAX_STEPS)]
    [SerializeField] private int _stepsFrame = 0;

    private const float MAX_STEPS_MOD = 50f;
    private const float MIN_STEPS_MOD = 1f;
    [Range(MIN_STEPS_MOD, MAX_STEPS_MOD)]
    [SerializeField] private int _stepsMod = 1;
    [SerializeField] private int _rezX = 512;
    // private Vector2 _rez {
    //     get { return new Vector2(_rezX, _rezX / ((float) Screen.width / Screen.height)); }
    //     set => _rez = value;
    // }
    private Vector2 _rez {
        get { return new Vector2(_rezX, _rezX); }
        set => _rez = value;
    }

    [SerializeField] private bool _dynamicVariables = false;
    [SerializeField] private bool _isPlaying = true;

    private const int MAX_PARTICLES_COUNT = 1000000;
    private const int MIN_PARTICLES_COUNT = 64;
    [Header("Rules")]
    [Range(MIN_PARTICLES_COUNT, MAX_PARTICLES_COUNT)]
    [SerializeField] private int _particlesCount = 1000;

    private const int MAX_GRID_SIZE = 512;
    private const int MIN_GRID_SIZE = 8;
    [Range(MIN_GRID_SIZE, MAX_GRID_SIZE)]
    [SerializeField] private int _gridSize = 32;

    private const int MAX_GRID_RANGE = 10;
    private const int MIN_GRID_RANGE = 1;
    [Range(MIN_GRID_RANGE, MAX_GRID_RANGE)]
    [SerializeField] private int _gridRange = 1;
    
    [SerializeField] private bool _useGrid = false;

    [System.Serializable]
    private class RDTexture
    {
        public RenderTexture texture;
        public string name;
    }

    [SerializeField] private  List<RDTexture> _rdTextures = new List<RDTexture>();
    private List<ComputeBuffer> _buffers;
    private List<RenderTexture> _textures;
    private ComputeBuffer _particlesBuffer, _particlesBufferRead, _gridBuffer;
    private RenderTexture _outTexture;

    private int _particlesRenderKernel, _renderKernel, _particlesKernel, _gridKernel, _resetGridKernel, _updateBufferKernel, _gridBufferSize;

    private void Awake()
    {
        Reset();
    }

    private void Update()
    {
        if (Time.frameCount % _stepsMod != 0 || !_isPlaying) return;
        for (int i = 0; i < _stepsFrame; i++)
            Step();
    }

    private void OnDestroy()
    {
        Release();
    }

    private RenderTexture CreateRenderTexture(RenderTextureFormat format)
    {
        RenderTexture rt = new RenderTexture((int) _rez.x, (int) _rez.y, 1, format);
        rt.enableRandomWrite = true;
        rt.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        rt.volumeDepth = 1;
        rt.filterMode = FilterMode.Point;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.autoGenerateMips = false;
        rt.useMipMap = false;
        rt.Create();
        _textures.Add(rt);
        return rt;
    }

    [EButton]
    public void Reset()
    {
        Release();
        _rdTextures.Clear();
        _outTexture = CreateRenderTexture(RenderTextureFormat.ARGBFloat);
        _rdTextures.Add(new RDTexture {texture = _outTexture, name = "outTexture"});

        _particlesRenderKernel = _shader.FindKernel("ParticlesRenderKernel");
        _particlesKernel = _shader.FindKernel("ParticlesKernel");
        _resetGridKernel = _shader.FindKernel("ResetGridKernel");
        _updateBufferKernel = _shader.FindKernel("UpdateBufferKernel");
        _renderKernel = _shader.FindKernel("RenderKernel");

        _particlesBuffer = new ComputeBuffer(_particlesCount, sizeof(float) * 8 + sizeof(int));
        _particlesBufferRead = new ComputeBuffer(_particlesCount, sizeof(float) * 8 + sizeof(int));

        _gridKernel = _shader.FindKernel("GridKernel");
        _gridBufferSize = ((int) _rez.x / _gridSize) * ((int) _rez.x / _gridSize) * 64;
        _gridBuffer = new ComputeBuffer(_gridBufferSize, sizeof(int));
        _buffers.Add(_gridBuffer);
        
        _buffers.Add(_particlesBuffer);
        _buffers.Add(_particlesBufferRead);

        GPUResetKernel();
    }

    private void GPUResetKernel()
    {
        SetRules();
        int _resetKernel = _shader.FindKernel("ResetParticlesKernel");
        _shader.SetBuffer(_resetKernel, "particlesBuffer", _particlesBuffer);
        _shader.SetBuffer(_resetKernel, "particlesBufferRead", _particlesBufferRead);
        _shader.Dispatch(_resetKernel, _particlesCount / 64, 1, 1);
    }

    [EButton]
    private void Step()
    {
        if (_dynamicVariables) SetRules();
        GPUParticlesKernel();
        UpdateBufferKernel();
        Render();
    }

    private void GPUParticlesKernel()
    {
        if (_useGrid)
        {
            _shader.SetBuffer(_resetGridKernel, "gridBuffer", _gridBuffer);
            _shader.Dispatch(_resetGridKernel, _gridBufferSize / 64, 1, 1);

            _shader.SetBuffer(_gridKernel, "particlesBufferRead", _particlesBufferRead);
            _shader.SetBuffer(_gridKernel, "gridBuffer", _gridBuffer);
            _shader.Dispatch(_gridKernel, _particlesCount / 64, 1, 1);
        }

        _shader.SetBuffer(_particlesKernel, "gridBuffer", _gridBuffer);
        _shader.SetBuffer(_particlesKernel, "particlesBuffer", _particlesBuffer);
        _shader.SetBuffer(_particlesKernel, "particlesBufferRead", _particlesBufferRead);

        _shader.Dispatch(_particlesKernel, _particlesCount / 64, 1, 1);
    }

    private void UpdateBufferKernel()
    {
        _shader.SetBuffer(_updateBufferKernel, "particlesBuffer", _particlesBuffer);
        _shader.SetBuffer(_updateBufferKernel, "particlesBufferRead", _particlesBufferRead);
        _shader.Dispatch(_updateBufferKernel, _particlesCount / 64, 1, 1);
    }

    private void Render()
    {
        GPURenderKernel();
        GPUParticlesRenderKernel();
        _image.texture = _outTexture;
    }

    private void GPURenderKernel()
    {
        int threadsx = (int) _rez.x / 32 < 1 ? 1 : (int) _rez.x / 32;
        int threadsy = (int) _rez.y / 32 < 1 ? 1 : (int) _rez.y / 32;
        _shader.SetTexture(_renderKernel, "outTexture", _outTexture);
        _shader.Dispatch(_renderKernel, threadsx, threadsy, 1);
    }

    private void GPUParticlesRenderKernel()
    {
        _shader.SetBuffer(_particlesRenderKernel, "particlesBufferRead", _particlesBufferRead);
        _shader.SetTexture(_particlesRenderKernel, "outTexture", _outTexture);

        _shader.Dispatch(_particlesRenderKernel, _particlesCount / 64, 1, 1);
    }

    private void SetRules()
    {
        _shader.SetVector("rez", _rez);
        _shader.SetInt("gridSize", _gridSize);
        _shader.SetInt("gridRange", _gridRange);
        _shader.SetInt("time", Time.frameCount);
        _shader.SetInt("useGrid", _useGrid ? 1 : 0);
        _shader.SetInt("particlesCount", _particlesCount);
    }

    public void Release()
    {
        if (_buffers != null)
            foreach (ComputeBuffer buffer in _buffers)
                if (buffer != null)
                    buffer.Release();
        
        _buffers = new List<ComputeBuffer>();
    
        if (_textures != null)
            foreach (RenderTexture texture in _textures)
                if (texture != null)
                    texture.Release();
        
        _textures = new List<RenderTexture>();
    }
}