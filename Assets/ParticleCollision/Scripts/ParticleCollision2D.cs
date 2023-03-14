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
    [SerializeField] private bool _dynamicVariables = false;
    [SerializeField] private bool _isPlaying = true;

    private const int MAX_SPATIAL_DIVISIONS = 30;
    private const int MIN_SPATIAL_DIVISIONS = 1;
    [Header("Spatial partitioning")]
    [Range(MIN_SPATIAL_DIVISIONS, MAX_SPATIAL_DIVISIONS)]
    [SerializeField] private int _spatialDivisions = 4;

    private const int MAX_SPATIAL_RANGE = 30;
    private const int MIN_SPATIAL_RANGE = 1;
    [Range(MIN_SPATIAL_RANGE, MAX_SPATIAL_RANGE)]
    [SerializeField] private int _spatialRange = 1;
    [SerializeField] private bool _useSpatialPartitioning = false;

    private const float MAX_BOUNCE = 5f;
    private const float MIN_BOUNCE = 0f;
    [Header("Rules")]
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

    private const float MAX_MASS_MATTERS = 1f;
    private const float MIN_MASS_MATTERS = 0f;
    [Range(MIN_MASS_MATTERS, MAX_MASS_MATTERS)]
    [SerializeField] private float _massMatters = 1f;

    private const float MAX_DIR_MULT = 5f;
    private const float MIN_DIR_MULT = 0f;
    [Range(MIN_DIR_MULT, MAX_DIR_MULT)]
    [SerializeField] private float _dirMult = 1f;

    [Header("Visuals")]
    [SerializeField] private int _rezX = 512;
    private Vector2 _rez;

    private const int MAX_PARTICLES_COUNT = 1000000;
    private const int MIN_PARTICLES_COUNT = 64;
    [Range(MIN_PARTICLES_COUNT, MAX_PARTICLES_COUNT)]
    [SerializeField] private int _particlesCount = 1000;

    private const int MAX_PARTICLE_SIZE = 20;
    private const int MIN_PARTICLE_SIZE = 2;
    [Range(MIN_PARTICLE_SIZE, MAX_PARTICLE_SIZE)]
    [SerializeField] private int _particleSize = 6;
    [SerializeField] private bool _randomSize = false;

    [SerializeField] private Color _particleColor = Color.blue;
    [SerializeField] private bool _randomColor = false;

    private const float MAX_COLOR_DECAY = 1f;
    private const float MIN_COLOR_DECAY = 0f;
    [Range(MIN_COLOR_DECAY, MAX_COLOR_DECAY)]
    [SerializeField] private float _colorDecay = 1f;

    private const float MAX_CIRCLE_SMOOTH = 1f;
    private const float MIN_CIRCLE_SMOOTH = 0f;
    [Range(MIN_CIRCLE_SMOOTH, MAX_CIRCLE_SMOOTH)]
    [SerializeField] private float _circleSmooth = 1f;

    private const float MAX_MOUSE_RADIUS = 1f;
    private const float MIN_MOUSE_RADIUS = 0f;
    [Header("Mouse interaction")]
    [Range(MIN_MOUSE_RADIUS, MAX_MOUSE_RADIUS)]
    [SerializeField] private float _mouseRadius = .1f;

    private const float MAX_MOUSE_STRENGTH = 5f;
    private const float MIN_MOUSE_STRENGTH = 0f;
    [Range(MIN_MOUSE_STRENGTH, MAX_MOUSE_STRENGTH)]
    [SerializeField] private float _mouseStrengthX = 1f;

    [Range(MIN_MOUSE_STRENGTH, MAX_MOUSE_STRENGTH)]
    [SerializeField] private float _mouseStrengthY = 1f;

    private const float MAX_MOUSE_RADIUS_MULTIPLIER = 5f;
    private const float MIN_MOUSE_RADIUS_MULTIPLIER = 0f;
    [Range(MIN_MOUSE_RADIUS_MULTIPLIER, MAX_MOUSE_RADIUS_MULTIPLIER)]
    [SerializeField] private float _mouseRadiusMultiplier = 1f;

    [System.Serializable]
    private class RDTexture
    {
        public RenderTexture texture;
        public string name;
    }

    [SerializeField] private  List<RDTexture> _rdTextures = new List<RDTexture>();
    private List<ComputeBuffer> _buffers;
    private List<RenderTexture> _textures;
    private ComputeBuffer _particlesBuffer, _particlesBufferRead, _spatialPartitionBuffer;
    private RenderTexture _outTexture;
    private Camera _camera;
    private int _particlesRenderKernel, _renderKernel, _particlesKernel, _spatialPartitionKernel, _resetSpatialPartitionKernel, _updateBufferKernel, _spatialPartitionBufferSize;

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
        _updateBufferKernel = _shader.FindKernel("UpdateBufferKernel");
        _renderKernel = _shader.FindKernel("RenderKernel");

        _particlesBuffer = new ComputeBuffer(_particlesCount, sizeof(float) * 8 + sizeof(int));
        _particlesBufferRead = new ComputeBuffer(_particlesCount, sizeof(float) * 8 + sizeof(int));

        _spatialPartitionKernel = _shader.FindKernel("SpatialPartitionKernel");
        _resetSpatialPartitionKernel = _shader.FindKernel("ResetSpatialPartitionKernel");
        int particlesPerCell = 128;
        _spatialPartitionBufferSize = (int) (_spatialDivisions * _spatialDivisions) * particlesPerCell;
        _spatialPartitionBuffer = new ComputeBuffer(_spatialPartitionBufferSize, sizeof(int));
        _buffers.Add(_spatialPartitionBuffer);
        
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
        HandleTouch();
        if (_dynamicVariables) SetRules();
        GPUParticlesKernel();
        UpdateBufferKernel();
        Render();
    }

    private void GPUParticlesKernel()
    {
        if (_useSpatialPartitioning)
        {
            _shader.SetBuffer(_resetSpatialPartitionKernel, "spatialPartitionBuffer", _spatialPartitionBuffer);
            _shader.Dispatch(_resetSpatialPartitionKernel, _spatialPartitionBufferSize / 64, 1, 1);

            _shader.SetTexture(_spatialPartitionKernel, "outTexture", _outTexture);
            _shader.SetBuffer(_spatialPartitionKernel, "particlesBufferRead", _particlesBufferRead);
            _shader.SetBuffer(_spatialPartitionKernel, "spatialPartitionBuffer", _spatialPartitionBuffer);
            _shader.Dispatch(_spatialPartitionKernel, _particlesCount / 64, 1, 1);
        }

        _shader.SetBuffer(_particlesKernel, "spatialPartitionBuffer", _spatialPartitionBuffer);
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
        _image.texture = _rdTextures[0].texture;
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
        _shader.SetInt("time", Time.frameCount);
        _shader.SetFloat("deltaTime", Time.deltaTime);
        _shader.SetInt("particlesCount", _particlesCount);

        _shader.SetInt("spatialDivisions", _spatialDivisions);
        _shader.SetInt("spatialRange", _spatialRange);
        _shader.SetInt("useSpatialPartitioning", _useSpatialPartitioning ? 1 : 0);

        Vector2 _mouseTrigger = new Vector2(Input.GetMouseButton(0) ? 1 : 0, Input.GetMouseButton(1) ? 1 : 0);
        Vector2 _mouseUV = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
        Cursor.visible = _mouseUV.x < 0 || _mouseUV.x > 1 || _mouseUV.y < 0 || _mouseUV.y > 1 ? true : false;

        _shader.SetVector("mouseUV", _mouseUV);
        _shader.SetVector("mouseTrigger", _mouseTrigger);
        _shader.SetVector("mouseStrength", new Vector2(_mouseStrengthX, _mouseStrengthY));
        _shader.SetFloat("mouseRadiusMultiplier", _mouseRadiusMultiplier);

        _shader.SetInt("randomColor", _randomColor ? 1 : 0);
        _shader.SetVector("particleColor", _particleColor);
        _shader.SetInt("randomSize", _randomSize ? 1 : 0);
        _shader.SetInt("particleSize", _particleSize);
        _shader.SetFloat("dirMult", _dirMult);
        _shader.SetFloat("massMatters", _massMatters);
        _shader.SetFloat("spacing", _spacing);
        _shader.SetFloat("friction", _friction);
        _shader.SetFloat("bounceWall", _bounceWall);
        _shader.SetFloat("mouseRadius", _mouseRadius);
        _shader.SetFloat("gravityForce", _gravityForce);
        _shader.SetFloat("bounceParticle", _bounceParticle);
        _shader.SetFloat("colorDecay", _colorDecay);
        _shader.SetFloat("circleSmooth", _circleSmooth);
    }

    private Vector2 GetThreadGroupSize()
    {
        float pageSize = 32f;
        float threadsx = Mathf.Ceil(_rez.x / pageSize);
        float threadsy = Mathf.Ceil(_rez.y / pageSize);

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

    private void HandleTouch()
    {
        // finger down reset
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            Reset();
    }
}