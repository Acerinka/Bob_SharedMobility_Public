using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public partial class BobController
    {
        private void DoIdleMotion()
        {
            Vector3 startWorldPos = HomeWorldPosition;
            bool atHomeBase = Vector3.Distance(lastVanishedPos, startWorldPos) < 0.1f;

            if (enableOrbitAtStart && atHomeBase)
            {
                DoOrbitMotion();
            }
            else
            {
                DoStationaryHover();
            }
        }

        private void DoOrbitMotion()
        {
            float time = Time.time;
            float angle = time * rotateSpeed;
            float xOffset = Mathf.Cos(angle) * rotateRadius;
            float zOffset = Mathf.Sin(angle) * rotateRadius;
            float waveY = _burstYOffset > 0.01f
                ? 0f
                : Mathf.Sin(time * floatSpeed) * floatAmplitude;

            transform.position = lastVanishedPos + new Vector3(xOffset, waveY + _burstYOffset, zOffset);

            Vector3 lookDir = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle));
            if (rotateSpeed < 0)
            {
                lookDir = -lookDir;
            }

            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        private void DoStationaryHover()
        {
            float time = Time.time;
            float waveY = _burstYOffset > 0.01f
                ? 0f
                : Mathf.Sin(time * floatSpeed) * floatAmplitude;

            transform.position = lastVanishedPos + new Vector3(0, waveY + _burstYOffset, 0);
            transform.localRotation = _initialRot;
        }

        private void DoMixerSimulation()
        {
            float finalBodyVol = Mathf.Clamp01(masterVolume + bodyOnlyVolume);
            float finalCoreVol = Mathf.Clamp01(masterVolume + coreOnlyVolume);

            if (_curBodyMat)
            {
                _curBodyMat.SetFloat(EnergyID, Mathf.Lerp(minEnergy, maxEnergy, finalBodyVol));
            }

            if (_curCoreMat)
            {
                _curCoreMat.SetFloat(CoreLightID, Mathf.Lerp(minCoreLight, maxCoreLight, finalCoreVol));
                _curCoreMat.SetFloat(CoreSpeedID, Mathf.Lerp(minCoreSpeed, maxCoreSpeed, finalCoreVol));
            }
        }

        private void SetBlendShapeWeight(float value)
        {
            if (skins == null) return;

            foreach (SkinSet skin in skins)
            {
                if (!skin.bodyObject) continue;

                SkinnedMeshRenderer mesh = skin.bodyObject.GetComponent<SkinnedMeshRenderer>();
                if (mesh)
                {
                    mesh.SetBlendShapeWeight(stretchBlendShapeIndex, value);
                }
            }
        }

        private void ForEachActiveBodyMesh(System.Action<SkinnedMeshRenderer> action)
        {
            if (skins == null || action == null) return;

            foreach (SkinSet skin in skins)
            {
                if (!skin.bodyObject || !skin.bodyObject.activeSelf) continue;

                SkinnedMeshRenderer mesh = skin.bodyObject.GetComponent<SkinnedMeshRenderer>();
                if (mesh)
                {
                    action.Invoke(mesh);
                }
            }
        }

        private void KillMotionTweens()
        {
            DOTween.Kill(transform);
            DOTween.Kill(this);
            DOTween.Kill(BobActionTweenId);
            DOTween.Kill(RemoteFloatTweenId);
            DOTween.Kill(FlightShapeTweenId);

            if (_restoreTimer != null)
            {
                _restoreTimer.Kill();
                _restoreTimer = null;
            }
        }
    }
}
