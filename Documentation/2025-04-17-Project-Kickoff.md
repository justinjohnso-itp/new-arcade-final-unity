# Starting the Arcade Cabinet Game: Infinite Steampunk Delivery

And just like that, it's time for the big project.
My role is developing the game itself in Unity. Duban is handling the art, and Angela is doing the physical cabinet fabrication.

We brainstormed a few different directions initially. We originally wanted to do something co-op, but quickly realized we needed to scope down to single-player to make this feasible for the semester. Our first real concept was a mashup of *Overcooked* and *Diner Dash* set in Transylvania, where you'd serve food while dodging vampires – tentatively called "Taco Nightmare" (yes, after my Discord handle). Then we pivoted to "Speedy Senders," which was inspired by the UFO 50 game *Onion Delivery* but with an added mechanic where you'd have to rummage through packages in the back of your truck. Mark gave us feedback that this felt like trying to build two separate games, which was fair. That led us to the current idea, trying to simplify and focus on the core loop with the available controls.

The physical setup we're building for has a vertical screen, a steering wheel, a vertically mounted rotary wheel (like you might see for volume control), and a single button.

Based on that, we've landed on an infinite-scroller concept. The player is a delivery driver. The truck moves forward automatically at a speed set by the game, which will ramp up over time. The steering wheel controls the truck's lateral movement on the road.

The core mechanic involves managing packages. There's an inventory, visualized as a stack on the side of the screen. As you drive, delivery zones appear, each marked with a symbol. Packages also have symbols. The rotary wheel is used to cycle through the inventory stack, highlighting different packages. The goal is to have the correct package (matching the zone's symbol) highlighted when you pass through a delivery zone.

Visually, it's going to be 2D isometric, kind of like the classic *Paperboy* ([https://en.wikipedia.org/wiki/Paperboy_(video_game)](https://en.wikipedia.org/wiki/Paperboy_(video_game))). My teammate is handling the art, aiming for a steampunk vibe. On the tech side, I'll be using Unity 2D, likely leaning heavily on isometric tilemaps.

Some other games that fed into this idea were *Icy Tower* ([https://en.wikipedia.org/wiki/Icy_Tower](https://en.wikipedia.org/wiki/Icy_Tower)) for its progression feel, and *Wilmot's Warehouse* ([https://store.steampowered.com/app/839870/Wilmots_Warehouse/](https://store.steampowered.com/app/839870/Wilmots_Warehouse/)) for the satisfaction of organizing and delivering specific items.

There are still a lot of open questions I need to figure out as I start building:
*   How does the inventory actually get filled? Do packages just appear, or do I need to pick them up?
*   Is there a win condition, or is it purely score-based?
*   How exactly does difficulty scale besides just speed? More complex zones? Faster package cycling needed?
*   Does time play a role? A timer?
*   Scoring: Points for correct deliveries, obviously. What about penalties? Delivering the wrong package? Missing a zone entirely?
*   Losing: How does the player fail? I'm thinking obstacles to dodge with the steering wheel. Maybe hitting obstacles damages the truck (lives), or maybe some obstacles mess with the inventory stack (shuffling packages?).
*   Levels: Is it one continuous run, or broken into stages/shifts?

We have a very rough ideas doc here ([https://docs.google.com/document/d/1oaBkfEi3oymEgV392tMevrXvMc19z39tzx_l02iLC68/edit?usp=sharing](https://docs.google.com/document/d/1oaBkfEi3oymEgV392tMevrXvMc19z39tzx_l02iLC68/edit?usp=sharing)), though a lot of it is from earlier brainstorming before we settled on this direction.

Some other things floating in my head:
*   I really like how *Icy Tower* changes the environment visuals as you get higher. I want to try and capture that feeling, maybe changing the scenery/biome based on score or distance.
*   Want to make sure the mechanics feel grounded in the steampunk theme. If packages magically appear, maybe there's a visible teleportation device on the truck.
*   Long-term, I want this to feel like a complete game experience – title screen, game over, high scores, etc. It needs to work in the cabinet, but I'd love for it to be playable standalone too. Maybe something I can keep working on after the class.
*   I'm planning to document the whole development process pretty thoroughly on my Notion blog here: [https://dusty-pineapple.notion.site/06090509687040a8a7381153743e3e5b?v=1139127f465d80069d80000c68e0824f](https://dusty-pineapple.notion.site/06090509687040a8a7381153743e3e5b?v=1139127f465d80069d80000c68e0824f)

Okay, time to actually open Unity and start putting something together.
