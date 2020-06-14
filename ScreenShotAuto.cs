/*
 * 使用方法：挂载到任意对象上，点击播放
 * 截图尺寸：遵循当前Game视图分辨率设定
 *
 * 待解决问题：
 *      目前虽然能把截图功能放在位移和旋转前方，但每次截图得到的结果竟仍然是位移和旋转后的截图
 *      因此不得不在第一帧截图前进行一次反向位移和旋转
 *      猜测为逻辑代码执行完后开始渲染，渲染后保存帧缓冲为图像，因此无论截图放在位移和旋转前后都会导致截图最后执行
 */


#if UNITY_EDITOR


using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;


internal enum AXIS
{
    X,
    Y,
    Z
};


internal enum COORD_SYSTEM
{
    LOCAL,
    WORLD
};


public class ScreenShotAuto : MonoBehaviour
{
    [Header("截图数量")]
    [Space(5)]
    [SerializeField] private uint count = 1;
    [Header("截图间隔")]
    [Space(5), Range(0.01f, 5.0f)]
    [SerializeField] private float interval = 0.1f;
    [Header("截图延迟(单位：秒)")]
    [Space(5), Range(0.0f, 10.0f)]
    [SerializeField] private float delay = 0.0f;
    private float intervalInit;

    [Space(40)]

    [Header("旋转坐标系")]
    [Space(5)]
    [SerializeField] private COORD_SYSTEM coordSysRot = COORD_SYSTEM.WORLD;
    [Header("旋转轴向")]
    [Space(5)]
    [SerializeField] private AXIS axis = AXIS.Y;
    [Header("旋转对象")]
    [Space(5)]
    [SerializeField] private Transform transformRot;
    [Header("旋转角度"), Range(-180.0f, 180.0f)]
    [Space(5)]
    [SerializeField] private float angle;

    [Space(40)]

    [Header("位移坐标系")]
    [Space(5)]
    [SerializeField] private COORD_SYSTEM coordSysMov = COORD_SYSTEM.WORLD;
    [Header("位移对象")]
    [Space(5)]
    [SerializeField] private Transform transformMov;
    [Header("位移量")]
    [Space(5)]
    [SerializeField] private Vector3 movement = new Vector3(0.0f, 0.0f, 0.0f);

    [Space(40)]

    [Header("默认保存文件夹(以“/”分隔路径，根目录为工程文件夹最顶层)")]
    [Space(5)]
    [SerializeField] private string defaultPath = "ScreenShot";
    [Space(10)]
    [Header("默认名称前缀")]
    [Space(5)]
    [SerializeField] private string defaultPrefix = "ScreenShot";
    [Space(10)]
    [Header("截图结束后自动打开截图文件夹")]
    [Space(5)]
    [SerializeField] private bool openFolder = true;
    private bool saved = false;


    private void Awake()
    {
        intervalInit = interval;
        interval = 0.0f;
        // 退位一次位移，抵消第一帧的第一次位移
        if (transformMov)
        {
            if (coordSysMov == COORD_SYSTEM.WORLD)
                transformMov.position = (transformMov.position - movement);
            else
                transformMov.Translate(-movement);
        }
        // 退位一次旋转，抵消第一帧的第一次旋转
        if (transformRot)
        {
            switch (axis) 
            {
                case AXIS.X:
                    if (coordSysRot == COORD_SYSTEM.WORLD)
                        transformRot.Rotate(new Vector3(-angle, 0, 0), Space.World);
                    else
                        transformRot.Rotate(new Vector3(-angle, 0, 0), Space.Self);
                    break;

                case AXIS.Y:
                    if (coordSysRot == COORD_SYSTEM.WORLD)
                        transformRot.Rotate(new Vector3(0, -angle, 0), Space.World);
                    else
                        transformRot.Rotate(new Vector3(0, -angle, 0), Space.Self);
                    break;

                case AXIS.Z:
                    if (coordSysRot == COORD_SYSTEM.WORLD)
                        transformRot.Rotate(new Vector3(0, 0, -angle), Space.World);
                    else
                        transformRot.Rotate(new Vector3(0, 0, -angle), Space.Self);
                    break;
            }
        }
    }


    void Update ()
    {
        // 截图启动倒计时
        if (delay <= 0)
        {
            // 未完全完成截图
            if (!saved)
            {
                // 两次截图间隔
                if (interval <= 0.0f)
                {
                    // 如果位移
                    if (transformMov)
                    {
                        if (coordSysMov == COORD_SYSTEM.WORLD)
                            transformMov.position = transformMov.position + movement;
                        else
                            transformMov.Translate(movement);
                    }
                    // 如果旋转
                    if (transformRot)
                    {
                        switch (axis) 
                        {
                            case AXIS.X:
                                if (coordSysRot == COORD_SYSTEM.WORLD)
                                    transformRot.Rotate(new Vector3(angle, 0, 0), Space.World);
                                else
                                    transformRot.Rotate(new Vector3(angle, 0, 0), Space.Self);
                                break;

                            case AXIS.Y:
                                if (coordSysRot == COORD_SYSTEM.WORLD)
                                    transformRot.Rotate(new Vector3(0, angle, 0), Space.World);
                                else
                                    transformRot.Rotate(new Vector3(0, angle, 0), Space.Self);
                                break;

                            case AXIS.Z:
                                if (coordSysRot == COORD_SYSTEM.WORLD)
                                    transformRot.Rotate(new Vector3(0, 0, angle), Space.World);
                                else
                                    transformRot.Rotate(new Vector3(0, 0, angle), Space.Self);
                                break;
                        }
                    }
                    // 旋转或位移后复位倒计时
                    interval = intervalInit;
                    count -= 1;
                    delay = 0.0f;
                    // 截图
                    string path = Capture();
                    if (count <= 0)
                    {
                        saved = true;
                        if (openFolder)
                            Explore(path);
                        EditorApplication.isPlaying = false;
                    }
                }
                interval -= Time.deltaTime;
            }
        }
        delay -= Time.deltaTime;
    }


    string Capture()
    {
        string projectPath = System.Environment.CurrentDirectory;
        System.DateTime timeStamp = System.DateTime.Now;
        string timeStampStr = timeStamp.Year + "年" +
                                                timeStamp.Month + "月" +
                                                timeStamp.Day + "日" +
                                                timeStamp.Hour + "时" +
                                                timeStamp.Minute + "分" +
                                                timeStamp.Second + "秒" +
                                                timeStamp.Millisecond + "毫秒";
        // 如果不存在文件夹就创建
        string absPath = projectPath + "/" + defaultPath;
        if (!Directory.Exists(absPath))
        {
            Directory.CreateDirectory(absPath);
        }
#if UNITY_5
        Application.CaptureScreenshot(absPath + "/" + defaultPrefix + "_" + timeStampStr + ".png");
#elif UNITY_2017
        ScreenCapture.CaptureScreenshot(absPath + "/" + defaultPrefix + "_" + timeStampStr + ".png");
#else
        ScreenCapture.CaptureScreenshot(absPath + "/" + defaultPrefix + "_" + timeStampStr + ".png");
#endif

        return absPath;
    }


     // 截屏后自动打开路径
    static void Explore(string path)
    {
#if UNITY_EDITOR_WIN
        // Windows下替换路径分隔符
        string winPath = path.Replace("/", "\\");
        // 控制台启动Explorer进入保存路径，目前仅测试过Windows7、Window10
        System.Diagnostics.Process.Start(winPath);
        Debug.Log("保存到\t" + winPath);
#else
        Debug.Log("保存到\t" + path);
#endif
    }
}


# endif
