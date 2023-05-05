using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class ParticleCollision2D : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private ComputeShader _shader;
    [SerializeField] private RawImage _image;

    [SerializeField] private bool _vSync = true;
    [SerializeField] private bool _unlimitedFPS = false;

    private const int MAX_FPS = 144;
    private const int MIN_FPS = 0;
    [Range(MIN_FPS, MAX_FPS)]
    [SerializeField] private int _fps = 60;

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
    [SerializeField] private bool _resetObstacles = false;

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

    private const float MAX_GRAVITY = .3f;
    private const float MIN_GRAVITY = 0f;
    [Header("Rules")]
    [Range(MIN_GRAVITY, MAX_GRAVITY)]
    [SerializeField] private float _gravityForce = 0.01f;

    private const float MAX_FRICTION = .3f;
    private const float MIN_FRICTION = 0f;
    [Range(MIN_FRICTION, MAX_FRICTION)]
    [SerializeField] private float _friction = 0.01f;

    private const float MAX_MASS_MATTERS = 1f;
    private const float MIN_MASS_MATTERS = 0f;
    [Range(MIN_MASS_MATTERS, MAX_MASS_MATTERS)]
    [SerializeField] private float _massMatters = 1f;

    private const float MAX_BOUNCE = 5f;
    private const float MIN_BOUNCE = 0f;
    [Range(MIN_BOUNCE, MAX_BOUNCE)]
    [SerializeField] private float _bounceWall = 0f;
    [Range(MIN_BOUNCE, MAX_BOUNCE)]
    [SerializeField] private float _bounceObstacles = 0f;
    [Range(MIN_BOUNCE, MAX_BOUNCE)]
    [SerializeField] private float _bounceParticle = 1f;
    
    private const float MAX_WALL_PORTION = 1f;
    private const float MIN_WALL_PORTION = .5f;
    [Range(MIN_WALL_PORTION, MAX_WALL_PORTION)]
    [SerializeField] private float _wallPortionX = 1f;
    [Range(MIN_WALL_PORTION, MAX_WALL_PORTION)]
    [SerializeField] private float _wallPortionY = 1f;

    private const float MAX_SPACING = 5f;
    private const float MIN_SPACING = 0f;
    [Range(MIN_SPACING, MAX_SPACING)]
    [SerializeField] private float _spacing = 1f;

    private const float MAX_DIR_MULT = 5f;
    private const float MIN_DIR_MULT = 0f;
    [Range(MIN_DIR_MULT, MAX_DIR_MULT)]
    [SerializeField] private float _dirMult = 1f;
    [SerializeField] private bool _collideWithObstacles = false;

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

    private const float MAX_COLOR_DECAY = 1f;
    private const float MIN_COLOR_DECAY = 0f;
    [Range(MIN_COLOR_DECAY, MAX_COLOR_DECAY)]
    [SerializeField] private float _colorDecay = 1f;


    [SerializeField] private bool _randomSize = false;
    [SerializeField] private bool _randomColor = false;
    [SerializeField] private Color _particleColor = Color.blue;
    [SerializeField] private Color _obstacleColor = Color.white;

    private const float MAX_CIRCLE_PERCENTAGE = 1f;
    private const float MIN_CIRCLE_PERCENTAGE = 0f;
    [Header("Circle render visuals")]
    [Range(MIN_CIRCLE_PERCENTAGE, MAX_CIRCLE_PERCENTAGE)]
    [SerializeField] private float _circleSmooth = 1f;

    private enum CircleRenderType
    {
        SOLID,
        OUTLINE,
        SOLID_AND_OUTLINE,
        STYLIZED_OUTLINE,
        SOLID_AND_STYLIZED_OUTLINE,
        SOLID_PERCENTAGE,
    }
    [SerializeField] private CircleRenderType _circleRenderType = CircleRenderType.SOLID;

    [Range(MIN_CIRCLE_PERCENTAGE, MAX_CIRCLE_PERCENTAGE)]
    [SerializeField] private float _circlePercentageX = 1f;

    [Range(MIN_CIRCLE_PERCENTAGE, MAX_CIRCLE_PERCENTAGE)]
    [SerializeField] private float _circlePercentageY = 1f;

    [Range(MIN_CIRCLE_PERCENTAGE, MAX_CIRCLE_PERCENTAGE)]
    [SerializeField] private float _circlePercentageZ = 1f;

    [Range(MIN_CIRCLE_PERCENTAGE, MAX_CIRCLE_PERCENTAGE)]
    [SerializeField] private float _circlePercentageW = 1f;

    private const float MAX_MOUSE_RADIUS = 1f;
    private const float MIN_MOUSE_RADIUS = 0f;
    [Header("Mouse interaction")]
    [SerializeField] private bool _useMouseToCreateObstacles = false;

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
    [SerializeField] private Color _mouseColor = Color.white;
    [SerializeField] private Color _mouseRightClickColor = Color.red;
    [SerializeField] private Color _mouseLeftClickColor = Color.green;

    [System.Serializable]
    private class RDTexture
    {
        public RenderTexture texture;
        public string name;
    }

    [SerializeField] private TextMeshProUGUI _fpsText;
    [SerializeField] private  List<RDTexture> _rdTextures = new List<RDTexture>();
    private List<ComputeBuffer> _buffers;
    private ComputeBuffer _particlesBuffer, _particlesBufferRead, _spatialPartitionBuffer;
    private RenderTexture _outTexture, _obstacleTexture;
    private Camera _camera;
    private int _renderKernel, _particlesKernel, _spatialPartitionKernel, _resetSpatialPartitionKernel, _updateBufferKernel, _spatialPartitionBufferSize;

    private void Awake()
    {
        _camera = FindObjectOfType<Camera>();
        Reset();
    }

    private void Update()
    {
        if (_fpsText != null)
            _fpsText.text = ((int) (1f / Time.unscaledDeltaTime)).ToString() + " fps";

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
        return rt;
    }

    [EButton]
    public void Reset()
    {
        Application.targetFrameRate = _unlimitedFPS ? -1 : _fps;
        QualitySettings.vSyncCount = _vSync || _unlimitedFPS ? 1 : 0;

        Release();
        _rez = new Vector2(_rezX, _rezX / _camera.aspect);
        
        _rdTextures.Clear();
        _outTexture = CreateRenderTexture(RenderTextureFormat.ARGBFloat);
        _rdTextures.Add(new RDTexture {texture = _outTexture, name = "outTexture"});

        if (_resetObstacles || _obstacleTexture == null)
            _obstacleTexture = CreateRenderTexture(RenderTextureFormat.ARGBFloat);

        _rdTextures.Add(new RDTexture {texture = _obstacleTexture, name = "obstacleTexture"});

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
        SetShaderParams();
        int _resetKernel = _shader.FindKernel("ResetParticlesKernel");
        _shader.SetBuffer(_resetKernel, "particlesBuffer", _particlesBuffer);
        _shader.SetBuffer(_resetKernel, "particlesBufferRead", _particlesBufferRead);
        _shader.Dispatch(_resetKernel, _particlesCount / 64, 1, 1);
    }

    [EButton]
    private void Step()
    {
        HandleKeys();
        if (_dynamicVariables) SetShaderParams();
        Render();
        GPUParticlesKernel();
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

        _shader.SetTexture(_particlesKernel, "outTexture", _outTexture);
        _shader.SetTexture(_particlesKernel, "obstacleTexture", _obstacleTexture);
        _shader.SetBuffer(_particlesKernel, "spatialPartitionBuffer", _spatialPartitionBuffer);
        _shader.SetBuffer(_particlesKernel, "particlesBuffer", _particlesBuffer);
        _shader.SetBuffer(_particlesKernel, "particlesBufferRead", _particlesBufferRead);
        _shader.Dispatch(_particlesKernel, _particlesCount / 64, 1, 1);
        UpdateBufferKernel();
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
        _image.texture = _rdTextures[0].texture;
    }

    private void GPURenderKernel()
    {
        Vector2 threads = GetThreadGroupSize();

        _shader.SetTexture(_renderKernel, "outTexture", _outTexture);
        _shader.SetTexture(_renderKernel, "obstacleTexture", _obstacleTexture);
        _shader.Dispatch(_renderKernel, (int) threads.x, (int) threads.y, 1);
    }

    private void SetShaderParams()
    {
        SetRules();
        SetVisuals();
        SetMouseInteraction();
        SetSpatialPartitioning();
    }

    private void SetRules()
    {   
        _shader.SetInt("time", Time.frameCount);
        _shader.SetFloat("deltaTime", Time.fixedDeltaTime);
        _shader.SetInt("collideWithObstacles", _collideWithObstacles ? 1 : 0);
        _shader.SetFloat("dirMult", _dirMult);
        _shader.SetFloat("massMatters", _massMatters);
        _shader.SetFloat("spacing", _spacing);
        _shader.SetFloat("friction", _friction);
        _shader.SetFloat("bounceWall", _bounceWall);
        _shader.SetFloat("bounceObstacles", _bounceObstacles);
        _shader.SetVector("wallPortion", new Vector2(_wallPortionX, _wallPortionY));
        _shader.SetFloat("gravityForce", _gravityForce);
        _shader.SetFloat("bounceParticle", _bounceParticle);
    }

    private void SetMouseInteraction()
    {
        Vector2 _mouseTrigger = new Vector2(Input.GetMouseButton(0) ? 1 : 0, Input.GetMouseButton(1) ? 1 : 0);
        Vector2 _mouseUV = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
        Cursor.visible = _mouseUV.x < 0 || _mouseUV.x > 1 || _mouseUV.y < 0 || _mouseUV.y > 1 ? true : false;

        _shader.SetVector("mouseUV", _mouseUV);
        _shader.SetVector("mouseRadius", new Vector2(_mouseRadius, _mouseRadiusMultiplier));
        _shader.SetVector("mouseTrigger", _mouseTrigger);
        _shader.SetVector("mouseStrength", new Vector2(_mouseStrengthX, _mouseStrengthY));
        _shader.SetVector("mouseColor", _mouseColor);
        _shader.SetVector("mouseLeftClickColor", _mouseLeftClickColor);
        _shader.SetVector("mouseRightClickColor", _mouseRightClickColor);
        _shader.SetInt("useMouseToCreateObstacles", _useMouseToCreateObstacles ? 1 : 0);
    }

    private void SetVisuals()
    {
        _rez = new Vector2(_rezX, _rezX / _camera.aspect);
        _shader.SetVector("rez", _rez);
        _shader.SetInt("particlesCount", _particlesCount);
        _shader.SetFloat("colorDecay", _colorDecay);
        _shader.SetFloat("circleSmooth", _circleSmooth);
        _shader.SetInt("particleSize", _particleSize);
        _shader.SetInt("randomSize", _randomSize ? 1 : 0);
        _shader.SetInt("randomColor", _randomColor ? 1 : 0);
        _shader.SetVector("particleColor", _particleColor);
        _shader.SetVector("obstacleColor", _obstacleColor);

        _shader.SetInt("circleRenderType", (int) _circleRenderType);
        _shader.SetVector("circlePercentage", new Vector4(_circlePercentageX, _circlePercentageY, _circlePercentageZ, _circlePercentageW));
    }

    private void SetSpatialPartitioning()
    {
        _shader.SetInt("spatialDivisions", _spatialDivisions);
        _shader.SetInt("spatialRange", _spatialRange);
        _shader.SetInt("useSpatialPartitioning", _useSpatialPartitioning ? 1 : 0);
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
    }

    private void HandleKeys()
    {
        if (Input.GetKeyDown(KeyCode.R))
            Reset();
        
        if (Input.GetKeyDown(KeyCode.Space))
            _useMouseToCreateObstacles = !_useMouseToCreateObstacles;
        
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
            _mouseRadius += 0.001f;
        
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
            _mouseRadius -= 0.001f;
    }
}