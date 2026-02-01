GhostRumble – Technical Design Document (TDD)

Team
	•	Farraz Monnajemi (Game Artist, Design & Writer)
	•	David Johansson (Game Programmer, Design & Writer)
	•	Leroy Joweihan (Design & Production, Audio & Writer)

⸻

1. High-Level Overview

Game Name: GhostRumble
Genre: Top-down Arena PvP
Players: 2 (local or online, 1v1)
Perspective: Top-down
Match Length: Short, fast-paced rounds (1–3 minutes typical)

GhostRumble is a fast, skill-based arena game where two ghost-like entities duel in a confined arena. Players are always in motion, gliding across the arena and orbiting each other using a pivot-based movement system. Combat focuses on calculated shots, spatial control, and timing rather than raw reflexes.

⸻

2. Core Design Pillars
	•	Constant Motion – Players gradually lose their speed when not moving.
	•	Precision over Spam – Shots can cancel each other out
	•	Mind Games – Orbiting, spacing, and baiting are core skills
	•	High Replayability – Short rounds, variable items, emergent encounters
	•	Mechanical Clarity – Simple rules, deep mastery

⸻

3. Core Gameplay Loop
	1.	Players spawn in the arena
	2.	Constant gliding movement begins immediately
	3.	Players orbit, chase, evade, and reposition
	4.	Players fire projectiles (ghost fists)
	5.	Projectiles may:
	•	Hit opponent → life lost
	•	Collide with other projectiles → neutralize
	•	Miss → continue or despawn
	6.	Items spawn periodically and can be collected
	7.	First player to reduce opponent to 0 lives wins

⸻

4. Player Mechanics

4.1 Movement System
	•	Players are always moving (minimum velocity enforced)
	•	Movement has a gliding / ice-skating feel
	•	Acceleration and deceleration are limited
	•	Directional input influences velocity rather than position

Orbit / Pivot Mechanic
	•	Each player can rotate around the opponent using an invisible pivot line
	•	Input (e.g. Q/E or controller equivalents) causes:
	•	Clockwise orbit
	•	Counter-clockwise orbit
	•	Orbit radius is dynamic and affected by speed

⸻

4.2 Speed Boost
	•	Temporary burst of speed
	•	Used for:
	•	Dodging
	•	Aggressive pushes
	•	Escaping pressure

Constraints:
	•	Cooldown-based
	•	Boost preserves movement direction

⸻

5. Combat System

5.1 Primary Attack – Ghost Fist Projectile
	•	Player fires a forward-moving projectile
	•	Projectile direction is based on current facing / velocity vector
	•	Projectiles persist for a short duration

5.2 Projectile Interaction
	•	Projectile vs Player → damage (lose 1 life)
	•	Projectile vs Projectile → both destroyed (neutralization)
	•	Projectile vs Arena → despawn or bounce (TBD)

This system rewards timing and prediction rather than volume of fire.

⸻

6. Health & Lives
	•	Each player starts with 5 lives
	•	Each successful hit removes 1 life
	•	No regeneration
	•	Optional: brief invulnerability frames after hit (TBD)

⸻

7. Items & Power-Ups

Items spawn at intervals in the arena.

7.1 Item Categories
	•	Offensive Items
	•	Multi-directional shots
	•	Explosive projectiles
	•	Piercing projectiles
	•	Utility Items
	•	Speed boosts
	•	Temporary stat increases

7.2 Item Rules
	•	Items are temporary
	•	Only one active item effect at a time (TBD)
	•	Clear visual/audio feedback on pickup

⸻

8. Player Stats (Light RPG Layer)

Stats slightly modify core mechanics without breaking balance.
	•	AGI (Agility)
	•	Movement speed
	•	Acceleration
	•	DEX (Dexterity)
	•	Attack speed
	•	Projectile speed

Stats are:
	•	Flat modifiers
	•	Item-influenced, not permanent progression (initially)

⸻

9. Arena Design
	•	Single-screen arena
	•	Clear boundaries
	•	Minimal obstacles (initially)
	•	Emphasis on open space and flow

Optional future variations:
	•	Moving obstacles
	•	Hazard zones
	•	Arena modifiers

⸻

10. Camera & Presentation
	•	Fixed top-down camera
	•	Entire arena visible at all times
	•	Strong visual contrast between players, projectiles, and items

⸻

11. Controls (Initial Proposal)

Keyboard
	•	Movement: WASD / Arrow Keys (directional influence)
	•	Orbit Left / Right: Q / E
	•	Shoot: Space / Mouse Button
	•	Speed Boost: Shift

Controller
	•	Left Stick: Directional movement
	•	Shoulder Buttons: Orbit left/right
	•	Face Button: Shoot
	•	Trigger: Speed boost

⸻

12. Audio & Feedback
	•	Distinct sound for:
	•	Shooting
	•	Projectile collision
	•	Player hit
	•	Item pickup
	•	Subtle motion trails reinforce speed and direction
	•	Arena (Spooky, Up-tempo, Orchestric (Vivaldi as Reference for example))

⸻

13. Technical Scope (Early Prototype)
	•	2D physics-based movement
	•	Deterministic projectile logic
	•	Minimal UI (lives, cooldowns)
	•	Local multiplayer first
	•	Online multiplayer (future scope)

⸻

14. Open Questions / TBD
	•	Projectile bounce vs despawn
	•	Invulnerability frames after hit
	•	Stackable vs single item effects
	•	Exact stat scaling values
	•	Online netcode model (lockstep vs rollback)

⸻

15. Vision Statement

GhostRumble aims to feel like a duel in constant motion — a tight, readable, high-skill arena where positioning, timing, and nerve decide the outcome more than raw execution speed.

⸻

Document intended to be living and iterated alongside prototyping.
