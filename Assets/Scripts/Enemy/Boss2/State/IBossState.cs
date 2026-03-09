using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBossState{
    void OnEnter(BossContext ctx);
    void OnUpdate(BossContext ctx);
    void OnExit(BossContext ctx);
}
