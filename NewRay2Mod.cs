using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ray2Mod;
using Ray2Mod.Components;
using Ray2Mod.Components.Text;
using Ray2Mod.Components.Types;
using Ray2Mod.Game;
using Ray2Mod.Game.Functions;
using Ray2Mod.Game.Structs.Material;
using Ray2Mod.Game.Structs.MathStructs;
using Ray2Mod.Game.Structs.SPO;
using Ray2Mod.Game.Types;
using Ray2Mod.Structs.Input;
using Ray2Mod.Utils;

/* To begin modding Rayman 2, you have to add Ray2Mod.dll as a reference to this project:
 * 1. Right click References in the Solution Explorer
 * 2. Click Browse and locate Ray2Mod.dll 
 * 3. Press OK to add the reference
 * 
 * Now you can run your mod by building this project and dragging the exported DLL onto ModRunner.exe
 * To automatically start the ModRunner when clicking Start, configure the following:
 * 1. Click Project -> <YourProject> Properties...
 * 2. Open the Debug configuration on the left
 * 3. As start action, select "Start external program" and Browse for ModRunner.exe
 * 4. Under start options, set the command line arguments to <YourProject>.dll (for example Ray2Mod_RollBoostPracticeTool.dll)
 * 
 * Now the ModRunner will start and inject your mod whenever you click start.
 */

namespace Ray2Mod_RollBoostPracticeTool
{
   public unsafe class NewRay2Mod : IMod
   {
      RemoteInterface ri;

      private World world;

      private int jumpHoldTimer;
      private int hoverWaitTimer;
      private int hoverPressTimer;

      private static float TOLERANCE = 0.05f;

      private const int perfectJumpHoldDuration = 27;
      private const int perfectHoverWaitDuration = 2;
      private const int perfectHoverHoldDuration = 1;

      enum State
      {
         Ground,
         Jumping,
         JumpReleased,
         HoverPressed,
         HoverReleased,
      }

      private State state;

      private bool onGround;
      private bool hovering;
      private bool jumpPressed;

      private Pointer<SuperObject> raymanSpo;

      void Loop()
      {
         var spos = world.GetSuperObjectsWithNames(world.ActiveDynamicWorld);
         if (raymanSpo == null && spos.ContainsKey("Rayman")) {
            raymanSpo = spos["Rayman"];
         }

         if (raymanSpo != null) {
            var gravity = raymanSpo.StructPtr->PersoData->dynam->DynamicsBase->DynamicsBlockBase.m_xGravity;

            var dsgVars = raymanSpo.StructPtr->PersoData->GetDsgVarList();

            onGround = Math.Abs(gravity - 9.81f) < TOLERANCE;
            hovering = *((byte*)dsgVars[9].valuePtrCurrent) == 15;

            jumpPressed = world.InputStructure->EntryActions[(int)EntryActionNames.Action_Sauter]->validCount > 0;

            switch (state) {
               case State.Ground:

                  if (!onGround) {
                     state = State.Jumping;
                     jumpHoldTimer = 0;
                     hoverWaitTimer = 0;
                     hoverPressTimer = 0;
                  }

                  break;
               case State.Jumping:

                  jumpHoldTimer++;

                  if (!jumpPressed) {
                     state = State.JumpReleased;
                  } else if (onGround) {
                     state = State.Ground;
                  }

                  break;
               case State.JumpReleased:

                  hoverWaitTimer++;

                  if (hovering) {
                     state = State.HoverPressed;
                  } else if (onGround) {
                     state = State.Ground;
                  }

                  break;
               case State.HoverPressed:

                  hoverPressTimer++;

                  if (onGround) {
                     state = State.Ground;
                  } else if (!jumpPressed) {
                     state = State.HoverReleased;
                  }

                  break;
               case State.HoverReleased:
                  if (onGround) {
                     state = State.Ground;
                  }

                  break;
            }
         }
      }

      unsafe void IMod.Run(RemoteInterface remoteInterface)
      {
         ri = remoteInterface;
         world = new World();

         ri.Log("Rayman 2 Perfect Hover Practice Tool");


         GlobalActions.PreEngine += Loop;
         GlobalActions.EngineStateChanged += (oldState,newState) => { raymanSpo = null; };

         string TimingFeedback(int f, int pf)
         {
            if (f < pf) {
               return $"{pf - f} too early";
            } else if (f > pf) {
               return $"{f - pf} too late";
            }

            return $"Perfect";
         }

         TextOverlay groundTimerText =
            new TextOverlay(
               (previousText) =>
               {
                  return $"Jump duration: {jumpHoldTimer} f ({TimingFeedback(jumpHoldTimer, perfectJumpHoldDuration)})";
               }, 10, 5, 740).Show();

         TextOverlay hoverStartTimeText =
            new TextOverlay(
               (previousText) =>
               {
                  return
                     $"Time before hover started: {hoverWaitTimer} f ({TimingFeedback(hoverWaitTimer, perfectHoverWaitDuration)})";
               }, 10, 5,
               780).Show();

         TextOverlay hoverEndTimeText =
            new TextOverlay(
                  (previousText) =>
                  {
                     return
                        $"Hover tap duration: {hoverPressTimer} f ({TimingFeedback(hoverPressTimer, perfectHoverHoldDuration)})";
                  }, 10, 5, 820)
               .Show();

         TextOverlay adviceText =
            new TextOverlay(
                  (previousText) =>
                  {
                     return
                        $"Perfect timings: {perfectJumpHoldDuration}, {perfectHoverWaitDuration}, {perfectHoverHoldDuration}";
                  }, 10, 5, 860)
               .Show();
      }
   }
}