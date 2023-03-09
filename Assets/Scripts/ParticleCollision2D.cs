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

    private const int MAX_GRID_SIZE = 512;
    private const int MIN_GRID_SIZE = 8;
    [Range(MIN_GRID_SIZE, MAX_GRID_SIZE)]
    [SerializeField] private int _gridSize = 32;

    private const int MAX_GRID_RANGE = 10;
    private const int MIN_GRID_RANGE = 1;
    [Range(MIN_GRID_RANGE, MAX_GRID_RANGE)]
    [SerializeField] private int _gridRange = 1;
    [SerializeField] private bool _useGrid = false;
    private Vector2 _rez;

    [SerializeField] private bool _dynamicVariables = false;
    [SerializeField] private bool _isPlaying = true;

    private const int MAX_PARTICLES_COUNT = 1000000;
    private const int MIN_PARTICLES_COUNT = 64;
    [Header("Rules")]
    [Range(MIN_PARTICLES_COUNT, MAX_PARTICLES_COUNT)]
    [SerializeField] private int _particlesCount = 1000;

    private const float MAX_BOUNCE = 5f;
    private const float MIN_BOUNCE = 0f;
    [Range(MIN_BOUNCE, MAX_BOUNCE)]
    [SerializeField] private float _bounceWall = 0f;
    
    [Range(MIN_BOUNCE, MAX_BOUNCE)]
    [SerializeField] private float _bounceParticle = 1f;

    private const float MAX_GRAVITY = .3f;
    private const float MIN_GRAVITY = 0f;
    [Range(MIN_GRAVITY, MAX_GRAVITY)]
    [SerializeField] private float _gravityForce = 0.01f;

    private const float MAX_FRICTION = .3f;
    private const float MIN_FRICTION = 0f;
    [Range(MIN_FRICTION, MAX_FRICTION)]
    [SerializeField] private float _friction = 0.01f;

    private const float MAX_SPACING = 5f;
    private const float MIN_SPACING = 0f;
    [Range(MIN_SPACING, MAX_SPACING)]
    [SerializeField] private float _spacing = 1f;

    private const float MAX_DIR_MULT = 5f;
    private const float MIN_DIR_MULT = 0f;
    [Range(MIN_DIR_MULT, MAX_DIR_MULT)]
    [SerializeField] private float _dirMult = 1f;

    private const int MAX_PARTICLE_SIZE = 10;
    private const int MIN_PARTICLE_SIZE = 1;
    [Range(MIN_PARTICLE_SIZE, MAX_PARTICLE_SIZE)]
    [SerializeField] private int _particleSize = 6;
    [SerializeField] private bool _randomSize = false;

    [SerializeField] private Color _particleColor = Color.blue;
    [SerializeField] private bool _randomColor = false;

    private const float MAX_MOUSE_RADIUS = 1f;
    private const float MIN_MOUSE_RADIUS = 0f;
    [Range(MIN_MOUSE_RADIUS, MAX_MOUSE_RADIUS)]
    [SerializeField] private float _mouseRadius = .1f;

    private const float MAX_MOUSE_STRENGTH = 5f;
    private const float MIN_MOUSE_STRENGTH = 0f;
    [Range(MIN_MOUSE_STRENGTH, MAX_MOUSE_STRENGTH)]
    [SerializeField] private float _mouseStrength = 1f;

    private const float MAX_MOUSE_RADIUS_MULTIPLIER = 5f;
    private const float MIN_MOUSE_RADIUS_MULTIPLIER = 0f;
    [Range(MIN_MOUSE_RADIUS_MULTIPLIER, MAX_MOUSE_RADIUS_MULTIPLIER)]
    [SerializeField] private float _mouseRadiusMultiplier = 1f;

    private const float MAX_COLOR_DECAY = 1f;
    private const float MIN_COLOR_DECAY = 0f;
    [Range(MIN_COLOR_DECAY, MAX_COLOR_DECAY)]
    [SerializeField] private float _colorDecay = 1f;

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
    private Camera _camera;
    private int _particlesRenderKernel, _renderKernel, _particlesKernel, _gridKernel, _resetGridKernel, _updateBufferKernel, _gridBufferSize;

    private void Awake()
    {
        _camera = FindObjectOfType<Camera>();
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
        _rez = new Vector2(_rezX, _rezX / _camera.aspect);

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
        Vector2 threads = GetThreadGroupSize();

        _shader.SetTexture(_renderKernel, "outTexture", _outTexture);
        _shader.Dispatch(_renderKernel, (int) threads.x, (int) threads.y, 1);
    }

    private void GPUParticlesRenderKernel()
    {
        _shader.SetBuffer(_particlesRenderKernel, "particlesBufferRead", _particlesBufferRead);
        _shader.SetTexture(_particlesRenderKernel, "outTexture", _outTexture);

        _shader.Dispatch(_particlesRenderKernel, _particlesCount / 64, 1, 1);
    }

    private void SetRules()
    {
        _rez = new Vector2(_rezX, _rezX / _camera.aspect);
        
        _shader.SetVector("rez", _rez);
        _shader.SetInt("gridSize", _gridSize);
        _shader.SetInt("gridRange", _gridRange);
        _shader.SetInt("time", Time.frameCount);
        _shader.SetInt("useGrid", _useGrid ? 1 : 0);
        _shader.SetInt("particlesCount", _particlesCount);

        Vector2 _mouseTrigger = new Vector2(Input.GetMouseButton(0) ? 1 : 0, Input.GetMouseButton(1) ? 1 : 0);
        Vector2 _mouseUV = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
        Cursor.visible = _mouseUV.x < 0 || _mouseUV.x > 1 || _mouseUV.y < 0 || _mouseUV.y > 1 ? true : false;

        _shader.SetVector("mouseUV", _mouseUV);
        _shader.SetVector("particleColor", _particleColor);
        _shader.SetInt("randomColor", _randomColor ? 1 : 0);
        _shader.SetVector("mouseTrigger", _mouseTrigger);
        _shader.SetFloat("mouseStrength", _mouseStrength);
        _shader.SetFloat("mouseRadiusMultiplier", _mouseRadiusMultiplier);
        _shader.SetFloat("dirMult", _dirMult);
        _shader.SetFloat("spacing", _spacing);
        _shader.SetFloat("friction", _friction);
        _shader.SetInt("particleSize", _particleSize);
        _shader.SetInt("randomSize", _randomSize ? 1 : 0);
        _shader.SetFloat("bounceWall", _bounceWall);
        _shader.SetFloat("mouseRadius", _mouseRadius);
        _shader.SetFloat("gravityForce", _gravityForce);
        _shader.SetFloat("bounceParticle", _bounceParticle);
        _shader.SetFloat("colorDecay", _colorDecay);
    }

    private Vector2 GetThreadGroupSize()
    {
        float pageSize = 32f;
        float threadsx = Mathf.Round(_rez.x / pageSize);
        float threadsy = Mathf.Round(_rez.y / pageSize);

        if (threadsx < 1) threadsx = 1;
        if (threadsy < 1) threadsy = 1;

        return new Vector2(threadsx, threadsy);
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