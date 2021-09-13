using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine.UI;
using UnityEngine.Events;



namespace djsoapyknuckles.FP_Walker

{
    public class FP_Walker : MVRScript
    {




        //UI
        #region Variables

        //private Camera _mainCamera;
        private float camera_pos_x;
        private FreeControllerV3 _rootControl;
        private JSONStorableFloat _offset;
        private bool _isMoving = false;
        private Camera _mainCamera;
        
        
        

        //public BVHPlayer animator;

        #endregion

        public override void Init()
        {
            try
            {
                _mainCamera = SuperController.singleton.OVRCenterCamera;
                _rootControl = containingAtom.mainController;
                SuperController.LogMessage("root control is at position x: " + _rootControl.transform.position.x.ToString());
                StartCoroutine(InitDeferred());
            }

            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }

        }

        private IEnumerator CheckMoving()
        {
            while (true)
            {
                Vector3 startPos = _mainCamera.transform.position;
                //SuperController.LogMessage("start: " + startPos.ToString());
                yield return new WaitForSeconds(0.25f);
                Vector3 finalPos = _mainCamera.transform.position;
                //SuperController.LogMessage("final: " + finalPos.ToString());
                if (_rootControl != null)
                {
                    if //(startPos.x != finalPos.x || startPos.z != finalPos.z)
                        (Math.Abs(startPos.x - finalPos.x) > 0.2 || Math.Abs(startPos.z - finalPos.z) > 0.2) //differentiate between rotational movement vs. linear movement
                    {

                       //play bvh animation here
                        SuperController.LogMessage("CheckMoving: animation should be playing");
                    }
                    else if (startPos.x == finalPos.x && startPos.z == finalPos.z)
                    {
                        //stop playing bvh animation here
                        SuperController.LogMessage("CheckMoving: playback should be stopped");
                        
                    }

                    else
                    {
                        SuperController.LogMessage("no -if- condition met: playback not started");
                    }

                }

            }
        }

        void FixedUpdate()
        {


            if (_rootControl.transform.position != _mainCamera.transform.position)
            {
                //translate rootControl to camera position in x/z
                _rootControl.transform.position = new Vector3(_mainCamera.transform.position.x, 0, _mainCamera.transform.position.z);

            }

            if (_rootControl.transform.rotation.eulerAngles.y != _mainCamera.transform.rotation.eulerAngles.y)
            {
                //rotate rootControl around y axix to match camera rotation
                _rootControl.transform.rotation = Quaternion.Euler(new Vector3(0, _mainCamera.transform.eulerAngles.y, 0));
            }
        }
       
        
        protected IEnumerator InitDeferred()
        {
            //may not need initDeffered
            yield return new WaitForEndOfFrame();
            //SuperController.LogMessage("in initDefered");
            yield return new WaitForEndOfFrame();
            while (SuperController.singleton.isLoading)
            {
                //SuperController.LogMessage("is loading...");
                yield return null;
            }
            //_mam = SuperController.singleton.motionAnimationMaster;


            SuperController.LogMessage(_mainCamera.ToString());

            StartCoroutine(CheckMoving());
            //SuperController.LogMessage("out of initDefered");
        }


    }
}
