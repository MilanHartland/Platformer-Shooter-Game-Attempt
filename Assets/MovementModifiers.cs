// using System.Collections.Generic;
// using MilanUtils;
// using static MilanUtils.TimedAction;
// using UnityEngine;

// [RequireComponent(typeof(PlayerMovement))]
// public class MovementModifiers : MonoBehaviour
// {
//     PlayerMovement pm;
//     Rigidbody2D rb;
//     Dictionary<string, dynamic> abilityVars = new();

//     GameObject canvas;

//     // Start is called once before the first execution of Update after the MonoBehaviour is created
//     void Start()
//     {
//         pm = GetComponent<PlayerMovement>();
//         rb = GetComponent<Rigidbody2D>();
//         DragDrop.dragInAction += DragInAction;
//         DragDrop.dragOutAction += DragOutAction;
//         canvas = GameObject.Find("Canvas");

//         abilityVars.Add("Dash", false); pm.onLand += () => { abilityVars["Dash"] = true; };
//     }

//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.R)) canvas.SetActive(!canvas.activeSelf);

//         foreach (DragDrop dd in DragDrop.slottedItems)
//         {
//             if (dd.name != "Movement") continue;

//             InfoTag tag = dd.GetComponent<InfoTag>();
//             string name = tag.name;
//             switch (name)
//             {
//                 case "Dash":
//                     if (abilityVars[name] && Input.GetKeyDown(KeyCode.LeftShift) && Input.GetAxisRaw("Horizontal") != 0f)
//                     {
//                         float xBefore = rb.linearVelocityX;

//                         rb.linearVelocityY = Input.GetAxisRaw("Vertical") * tag.value;
//                         if (Mathf.Abs(rb.linearVelocityX) >= pm.maxMoveSpeed + tag.value) rb.linearVelocityX += Input.GetAxisRaw("Horizontal") * tag.value;
//                         else rb.linearVelocityX = Input.GetAxisRaw("Horizontal") * (pm.maxMoveSpeed + tag.value);

//                         Vector2 vel = rb.linearVelocity;
                        
//                         RepeatActionDuringUntil(0.5f,
//                         () => {rb.linearVelocity = vel; return pm.grounded || pm.touchingWall; },
//                         () => { rb.linearVelocityX = xBefore; });

//                         abilityVars[name] = false;
//                     }
//                     break;

//                 default: Debug.LogError($"No valid Movement found ({tag.name})"); break;
//             }
//         }
//     }

//     void DragInAction(DragDrop dd) { ApplyChanges(dd, true); }
//     void DragOutAction(DragDrop dd) { ApplyChanges(dd, false); }

//     void ApplyChanges(DragDrop dd, bool add)
//     {
//         if (dd.name.Contains("Modifier")) ApplyModifiers(dd.GetComponent<InfoTag>(), add);
//         else if (dd.name.Contains("Movement")) return;
//         else Debug.LogError($"No valid DragDrop name found ({dd.name})");
//     }

//     void ApplyModifiers(InfoTag tag, bool add)
//     {
//         switch (tag.name)
//         {
//             case "Jump Height":
//                 pm.jumpVel += add ? tag.value : -tag.value;
//                 break;
//             default: Debug.LogError($"No valid Modifier found ({tag.name})"); break;
//         }
//     }
// }
