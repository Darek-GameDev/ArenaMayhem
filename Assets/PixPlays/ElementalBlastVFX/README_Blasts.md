# Elemental Blasts VFX Package, version 1.0.1
24/09/2024
© 2023-2033 - Pix Plays Studio

IMPORTANT
--------------------
If your project is using URP, extract the package found inside named ElementalAoe_URP
If you have no need for Built In or URP render pipelines, it is safe to delete the folders tagged with _BuiltIn or _URP depending on which version you want to remove from the project

Asset is compatible with URP and Built In Render Pipeline.
This package contains 4 Elemental Blasts and their accompanying effects.

PREFABS
--------------------
    They are located in theirs respective folders under "PixPlays/ElementalBlast/"

TESTING
--------------------
    -To test the package open the “DemoBlastsScene” located in “PixPlays/ElementalBlast/”
	-Press the "Activate" button key to activate effects.

SCRIPTS
--------------------
VfxSystem

	Includes a VfxSystem. The system is designed to controll all types of VFX. It is made up from the scripts in the
	PixPlays/Components/Scripts/VfxSystem folder.

	"VfxData.cs" is used as a common class to keep the Vfx data needed to display and controll the effect correctly.
	- float _duration the lifetime of the effect
	- float _radius the radius of the effect if the effect is using a radius (Aoe effect). Ignored for effect that dont (ex. Projectile effect)
	- Transform _sourceTransform The source of the Vfx, the point from where it is cast.
	- Transform _targetTransform The target of the Vfx, the point to where it should point or go to.
	- Vector3 _sourcePos The static source of the Vfx,
	- Vector3 _targetPos The static target of the Vfx,
	- Vector3 Source Returns the position of the _sourceTransform if assigned else returns _sourcePos;
	- Vector3 Target Returns the position of the _targetTransform if assigned else returns _targetPos;

	The base class "BaseVfx.cs" is used to play and stop as well as dispose of effects.
	- float _SafetyDestroy Destroy the object after a certan time in case user error keeps it active.
	- float _DestroyDelay Wait for effect to finish stopping before destroying the GameObject
	- VfxData _data The data used to controll and set the parameters of the vfx such as Duration, Scale, Source, Target.
	-Play(VfxData data) is used to play the vfx.
	-Stop() is used to stop the effect
	From the "BaseVfx.cs" class we have derived specific effect classes to controll various typs of VFX.

	"LocationVfx.cs" is the class used for single location effects. The effects that dont need to move. (ex. Aoe effect)
	
	"ProjectileVfx.cs" is the class used for projectile effect. It has a Cast, Hit, and Projectile parameter. It controlls the movement of the projectile through space.
	
	"Shield.cs" is a class used to controll various Shield type effects.
	
	"BeamVfx.cs" is a class used to controll various Beam type effects;

	"VfxReference.cs" a reference script used as a base in order to store various types of different references of effect.Like PlayableDirector, ParticleSystem. Controll the vfx by Play() and Stop() functions that are defined in inherited scripts.
	
	"ParticleSystemVfx.cs" is a script used to store a ParticleSystem as a Vfx reference.
	
	"PlayableVfx.cs"  is a script used to store a PlayableDirector as a Vfx reference.

	"ParticleSystemScaleLifetime.cs" is used as an extention to a ParticleSystem to extend the lifetimes of the particles depending on the Object scale. The particle system must be scaled in Local space.
	
	"ParticleSystemStartStopLifetime.cs" is used as an extention to a ParticleSystem to modify the Lifetime of the particles when Playing and Stopping the system to different values.
	
	"TrailScaleWithHierarchy.cs" is used as an extention to a TrailRenderer to modify the Width of the trail depenging on the Objects scale x value.
	
Demo Scene Scripts:
	Included are also scripts for controlling the character and the Vfx:
	
	"BindingPoints.cs" is used to assign Transform points to BindingPointTypes which are returned depending on the spell configuration.
	"Character.cs" is used to controll the characters animation, Target, and BindingPoints.
	
	"VFXTester.cs" is a script used to instantiate and display various effects. It is the main class used to Demo the effects.

USAGE
--------------------
	1. Place the effect on the scene (Either by instantiating or by having it already present)
	2. Call the Play() function of the BaseVfx script attached to the Effect GameObject
	3. To stop call the Stop() function of the BaseVfx script attached to the Effect GameObject

To help you see the functionality in the "DemoBlastsScene" there is a "VFXTesters.cs" script that demonstrates the functions.


HELP?
--------------------
For any suggestions, problems, or help contact us at:
support@pixplays.studio 

THANK YOU FOR DOWNLOADING, WE HOPE YOU ENJOY OUR PACKAGE!
PLEASE LEAVE A REVIEW OR RATE THE PACKAGE IF YOU FIND IT USEFUL!
We will be very greatfull.

RELEASE NOTES
-------------
1.0.0
-Initial release


