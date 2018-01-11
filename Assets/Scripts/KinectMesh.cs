using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;

public class KinectMesh : MonoBehaviour {

    private KinectSensor _Sensor;
    private DepthFrameReader _Reader;
    private CoordinateMapper _Mapper;

    private ushort[] _Data;

    private Mesh _Mesh;
    private Vector3[] _Vertices;
    private Vector2[] _UV;
    private int[] _Triangles;

    // Only works at 4 right now
    private const int _DownsampleSize = 2;
    public  float _Scale;
    public Vector3 _Offset;
    private const int _Speed = 50;

    [Range(0,1)]
    public float motionSmooth;


    ColorSpacePoint[] colorSpace;
    CameraSpacePoint[] camSpace;

    // Use this for initialization
    void Start () {
        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null)
        {
            _Mapper = _Sensor.CoordinateMapper;
            var frameDesc = _Sensor.DepthFrameSource.FrameDescription;

            _Reader = _Sensor.DepthFrameSource.OpenReader();
            _Data = new ushort[_Sensor.DepthFrameSource.FrameDescription.LengthInPixels];

            colorSpace = new ColorSpacePoint[_Data.Length];
            camSpace = new CameraSpacePoint[_Data.Length];


            // Downsample to lower resolution
            CreateMesh(frameDesc.Width / _DownsampleSize, frameDesc.Height / _DownsampleSize);

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }


    }

    void CreateMesh(int width, int height)
    {
        _Mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _Mesh;

        _Vertices = new Vector3[width * height];

        _UV = new Vector2[width * height];
        _Triangles = new int[6 * ((width - 1) * (height - 1))];

        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                _Vertices[index] = new Vector3(x, -y, 0);
                _UV[index] = new Vector2(((float)x / (float)width), ((float)y / (float)height));

                // Skip the last row/col
                if (x != (width - 1) && y != (height - 1))
                {
                    int topLeft = index;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + width;
                    int bottomRight = bottomLeft + 1;

                    _Triangles[triangleIndex++] = topLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomRight;
                }
            }
        }

        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.triangles = _Triangles;
        _Mesh.RecalculateNormals();


    }

    // Update is called once per frame
    void Update()
    {
        if (_Sensor == null) return;

        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                frame.CopyFrameDataToArray(_Data);
                frame.Dispose();
                frame = null;
                RefreshData();
            }
        }

        //gameObject.GetComponent<Renderer>().material.mainTexture = _ColorManager.GetColorTexture();
    }




    private void RefreshData()
    {
        var frameDesc = _Sensor.DepthFrameSource.FrameDescription;


        _Mapper.MapDepthFrameToCameraSpace(_Data, camSpace);
        _Mapper.MapDepthFrameToColorSpace(_Data, colorSpace);

        for (int y = 0; y < frameDesc.Height; y += _DownsampleSize)
        {
            for (int x = 0; x < frameDesc.Width; x += _DownsampleSize)
            {
                int indexX = x / _DownsampleSize;
                int indexY = y / _DownsampleSize;
                int realIndex = y * frameDesc.Width + x;
                int smallIndex = (indexY * (frameDesc.Width / _DownsampleSize)) + indexX;

                //double avg = GetAvg(_Data, x, y, frameDesc.Width, frameDesc.Height);
                //avg = avg * _DepthScale + _Offset.z;


                //_Vertices[smallIndex].z = (float)avg;

                CameraSpacePoint p = camSpace[realIndex];
                Vector3 targetP = (new Vector3(p.X, p.Y, p.Z) + _Offset) * _Scale;

                if(float.IsInfinity(_Vertices[smallIndex].z))
                {
                    _Vertices[smallIndex] = targetP;
                }
                else
                {
                    _Vertices[smallIndex] = _Vertices[smallIndex] + (targetP - _Vertices[smallIndex]) * motionSmooth;// _OldVertices[smallIndex] + (targetP - _OldVertices[smallIndex]) / motionSmooth ;
                }


                // Update UV mapping with CDRP
                var colorSpacePoint = colorSpace[(y * frameDesc.Width) + x];
                _UV[smallIndex] = new Vector2(colorSpacePoint.X*1.0f / 1920, colorSpacePoint.Y*1.0f / 1080);
            }
        }

        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.triangles = _Triangles;
        _Mesh.RecalculateNormals();
        

    }

    private double GetAvg(ushort[] depthData, int x, int y, int width, int height)
    {
        double sum = 0.0;

        for (int y1 = y; y1 < y + 4; y1++)
        {
            for (int x1 = x; x1 < x + 4; x1++)
            {
                int fullIndex = (y1 * width) + x1;

                if (depthData[fullIndex] == 0)
                    sum += 4500;
                else
                    sum += depthData[fullIndex];

            }
        }

        return sum / 16;
    }

    private void OnDestroy()
    {
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }

        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }
}
