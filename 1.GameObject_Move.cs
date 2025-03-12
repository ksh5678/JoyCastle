
/*
* 1. 核心实现思路
* 全局动画管理器：避免重复创建GameObject和组件，通过单例模式或静态类统一管理所有动画的更新，避免每个对象单独调用Update方法造成性能浪费。
* 基于时间的插值计算：在Update中根据Time.deltaTime更新动画进度，结合缓动函数控制移动速度变化。
* 内存优化：使用结构体AnimationData存储动画参数，减少堆内存分配；避免频繁的new操作，通过对象池或数组复用动画数据。
* 避免闭包和装箱：直接传递参数，不使用委托或object类型。
* 也可以使用协程实现，主逻辑，但是还需要额外维护协程的重复调用以及开始/关闭 等逻辑。
*/
using UnityEngine;
using System.Collections.Generic;

public enum EaseType { Linear, EaseIn, EaseOut, EaseInOut }

public struct AnimationData
{
    public Transform target;
    public Vector3 startPos;
    public Vector3 endPos;
    public float duration;
    public float elapsedTime;
    public bool pingpong;
    public EaseType easeType;
    public bool isForward; // 当前移动方向（正向：start→end，反向：end→start）
}

public class AnimationManager : MonoBehaviour
{
    private static AnimationManager _instance;
    private List<AnimationData> _activeAnimations = new List<AnimationData>();

    public static void AddAnimation(AnimationData data)
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("AnimationManager");
            _instance = go.AddComponent<AnimationManager>();
        }
        _instance._activeAnimations.Add(data);
    }

    void Update()
    {
        for (int i = _activeAnimations.Count - 1; i >= 0; i--)
        {
            AnimationData data = _activeAnimations[i];
            data.elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(data.elapsedTime / data.duration);

            // 应用缓动函数
            t = ApplyEase(t, data.easeType);

            // 计算位置
            Vector3 currentPos = data.isForward ?
                Vector3.Lerp(data.startPos, data.endPos, t) :
                Vector3.Lerp(data.endPos, data.startPos, t);
            data.target.position = currentPos;

            // 判断动画是否完成
            if (data.elapsedTime >= data.duration)
            {
                if (data.pingpong)
                {
                    data.isForward = !data.isForward;
                    data.elapsedTime = 0f;
                }
                else
                {
                    _activeAnimations.RemoveAt(i);
                    continue;
                }
            }
            _activeAnimations[i] = data;
        }
    }

    private float ApplyEase(float t, EaseType type)
    {
        switch (type)
        {
            case EaseType.EaseIn: return t * t;
            case EaseType.EaseOut: return 1 - (1 - t) * (1 - t);
            case EaseType.EaseInOut: return t < 0.5f ? 2 * t * t : 1 - 2 * (1 - t) * (1 - t);
            default: return t; // Linear
        }
    }
}


// 其他类调用函数。
Move(gameObject, Vector3.zero, new Vector3(5, 0, 0), 3f, true, EaseType.EaseInOut);

public static void Move(GameObject gameObject, Vector3 begin, Vector3 end, float time, bool pingpong, EaseType easeType = EaseType.Linear)
{
    AnimationData data = new AnimationData
    {
        target = gameObject.transform,
        startPos = begin,
        endPos = end,
        duration = time,
        elapsedTime = 0f,
        pingpong = pingpong,
        easeType = easeType,
        isForward = true
    };
    AnimationManager.AddAnimation(data);
}