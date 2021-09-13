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
        private Camera _mainCamera;

        //BHVPlayer Variables
        string prevFolder = "";
        private static float footbaseheight = -1.0f;
        private static int startFrame = 0;
        private static Vector3 basecontrolpos = new Vector3(0.0f, 0.0f, 0.0f); //this may be why rootControl transform resets to 0,0,0 on load
        protected JSONStorableBool turnOffHeadandNeck;
        BvhFile _bvhFiletoPlayForWalkForward = null;
        BvhFile _bvhFiletoPlayForWalkBackward = null;
        BvhFile _bvhFiletoPlayForWalkLeft = null;
        BvhFile _bvhFiletoPlayForWalkRight = null;
        
        private static Atom person;
        private static FreeControllerV3 personController;
        private static FreeControllerV3 lShoulderController;
        private static FreeControllerV3 rShoulderController;
        private static FreeControllerV3 lArmController;
        private static FreeControllerV3 rArmController;
        private static FreeControllerV3 lElbowController;
        private static FreeControllerV3 rElbowController;
        private static FreeControllerV3 lHandController;
        private static FreeControllerV3 rHandController;
        private static FreeControllerV3 abdomenController;
        private static FreeControllerV3 abdomenLowerController;
        private static FreeControllerV3 pelvisController;
        private static FreeControllerV3 hipController;
        private static FreeControllerV3 chestController;
        private static FreeControllerV3 neckController;
        private static FreeControllerV3 headController;
        private static FreeControllerV3 lBreastController;
        private static FreeControllerV3 rBreastController;
        private static FreeControllerV3 lHipController;
        private static FreeControllerV3 rHipController;
        private static FreeControllerV3 lKneeController;
        private static FreeControllerV3 rKneeController;
        private static FreeControllerV3 lFootController;
        private static FreeControllerV3 rFootController;
        private static FreeControllerV3 lToeController;
        private static FreeControllerV3 rToeController;

        //Dictionaries
        Dictionary<string, FreeControllerV3> controllerMap;
        Dictionary<string, MotionAnimationControl> macMap;
        Dictionary<string, Transform> bones;
        Dictionary<string, Vector3> tposeBoneOffsets = null;
        Dictionary<string, string> cnameToBname = new Dictionary<string, string>() {
        { "hipControl", "hip" },
        { "headControl", "head" },
        { "chestControl", "chest" },
        { "lHandControl", "lHand" },
        { "rHandControl", "rHand" },
        { "lFootControl", "lFoot" },
        { "rFootControl", "rFoot" },
        { "lKneeControl", "lShin" },
        { "rKneeControl", "rShin" },
        { "neckControl", "neck" },
        { "lElbowControl", "lForeArm" },
        { "rElbowControl", "rForeArm" },
        { "lArmControl", "lShldr" },
        { "rArmControl", "rShldr" },
        // Additional bones
        { "lShoulderControl", "lCollar" },
        { "rShoulderControl", "rCollar" },
        { "abdomenControl", "abdomen" },
        { "abdomen2Control", "abdomen2" },
        { "pelvisControl", "pelvis" },
        { "lThighControl", "lThigh" },
        { "rThighControl", "rThigh" },
            // { "lToeControl", "lToe" },
            // { "rToeControl", "rToe" },
        };
        float elapsed = 0;
        int frame = 0;
        bool playing = false;
        //bool baking = false;
        //bool reverse = false;
        bool isUpdating = false;
        bool loopPlay = false;
        //bool loopBake = false;
        //bool pingpongPlay = false;
        //bool pingpongBake = false;
        bool onlyHipTranslation = true;
        bool translationIsDelta = false;
        float frameTime;

        // Apparently we shouldn't use enums because it causes a compiler crash
        const int translationModeOffsetPlusFrame = 0;
        const int translationModeFrameOnly = 1;
        const int translationModeInitialPlusFrameMinusOffset = 2;
        const int translationModeInitialPlusFrameMinusZero = 3;

        int translationMode = translationModeInitialPlusFrameMinusZero;


        #endregion

        public override void Init()
        {
            try
            {
                _mainCamera = SuperController.singleton.OVRCenterCamera;
                _rootControl = containingAtom.mainController;
                SuperController.LogMessage("root control is at position x: " + _rootControl.transform.position.x.ToString());
                StartCoroutine(InitDeferred());

                #region User Interface

                CreateButton("Select BVH File for Walk Forward").button.onClick.AddListener(() => {
                    if (prevFolder == "")
                        prevFolder = SuperController.singleton.savesDir + @"Animation\"; //updated initial path
                    SuperController.singleton.NormalizeMediaPath(prevFolder); //normalize the previous folder path
                    SuperController.singleton.GetMediaPathDialog((string path) => {
                        Load(path);
                        footbaseheight = lFootController.followWhenOff.position.y;
                    }, "bvh", prevFolder, true); //set showDirs to true
                });

                var modes = new List<string>();
                modes.Add("Offset + Frame (DAZ)");
                modes.Add("Frame only");
                modes.Add("Initial + Frame - Offset (MB)");
                modes.Add("Initial + Frame - Frame[0] (CMU)");
                var uiTranslationMode = new JSONStorableStringChooser("transmode", modes, modes[translationMode], "Translation Mode", (string val) => {
                    translationMode = modes.FindIndex((string mode) => { return mode == val; });
                    if ((translationMode == translationModeInitialPlusFrameMinusOffset || translationMode == translationModeInitialPlusFrameMinusZero) && tposeBoneOffsets == null)
                    {
                        // We need t-pose measurements, and don't have them yet
                        RecordOffsets();
                        CreateControllerMap();
                    }
                });
                CreatePopup(uiTranslationMode);
                turnOffHeadandNeck = new JSONStorableBool("Turn Off Neck and Head", false);
                RegisterBool(turnOffHeadandNeck);
                CreateToggle(turnOffHeadandNeck, false);
                #endregion
            }
             
     
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }

        }


        void UpdatePrevFolder(string path)
        {

            prevFolder = path.Substring(0, path.LastIndexOfAny(new char[] { '/', '\\' })) + @"\";
        }

        public void Load(string path)
        {
            _bvhFiletoPlayForWalkForward = new BvhFile(path);
            //uiAnimationPos.SetVal(0);
            //UpdateStatus();
            //UpdateSpeed(uiAnimationSpeed.val);
            UpdatePrevFolder(path);
            
        }

        void RecordOffsets()
        {
            containingAtom.ResetPhysical();
            //CreateShadowSkeleton();     // re-create
            tposeBoneOffsets = new Dictionary<string, Vector3>();
            foreach (var item in bones)
                tposeBoneOffsets[item.Key] = item.Value.localPosition;
        }

        void CreateControllerMap()
        {
            controllerMap = new Dictionary<string, FreeControllerV3>();
            foreach (FreeControllerV3 controller in containingAtom.freeControllers)
                controllerMap[controller.name] = controller;

            foreach (var item in cnameToBname)
            {
                var c = controllerMap[item.Key];
                if ((item.Key != "headControl" && item.Key != "neckControl") || turnOffHeadandNeck.val == false)
                {
                    c.currentRotationState = FreeControllerV3.RotationState.On;
                    c.currentPositionState = FreeControllerV3.PositionState.On;
                }
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
