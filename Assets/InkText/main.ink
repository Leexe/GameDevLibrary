=== Testing ===

#nm_none
"Hello, I'm the Player" #chl_player+pop #nm_Player #d_player
"I'm another character to the right" #chl_player+bounce #nm_Dude #chr_dude_laugh+shakevertical #d_dude
The other character strikes the player character. #chl_player+shake #chc_dude_concentrated #nm_Player #d_player
The left character disappears... #chr_player_clear #chl_dude_laugh+bounce #nm_none
End #nm_none
-> END

=== Testing_Animations ===

#nm_none

Spawning Character #chc_player

Testing Standard Shake. #an_player_shake
Testing Inline Syntax (Sprite Change + Shake). #chc_player_laugh+shake

Testing Hop with Parameters (Height 20, Duration 0.5s). #an_player_hop_20_0.5
Testing Reverse Hop. #an_player_reversehop

Testing Bounce on Dude. #chr_dude+bounce
Testing Flash on Character. #chc_player+flash
Testing Dodge on Character. #chc_player+dodge
Testing Pop on player. #chc_player+pop
Testing Vertical Shake on Dude. #chr_dude+shakevertical

Clearing Characters. #chl_player_clear #chr_dude_clear
Tests Complete.
-> END

=== Testing_Voices ===

#nm_none
Testing dialogue voice system.

#chc_player #nm_Player
"Hello! This line should play player's voice." #d_player
"Another line from player with voice." #d_player

#chr_player #nm_Character1
"Hi there! This is a character speaking." #d_player

#nm_none
This is narration. No voice should play here.

#nm_Player
"Back to player again!" #d_player

#nm_none
Voice test complete.
-> END

=== Testing_TextAnimator ===

#nm_none
Testing Text Animator Effects.

Basic Wiggle: <wiggle>Wiggle Wiggle</wiggle>
High Intensity Wiggle: <wiggle a=2>INTENSE WIGGLE</wiggle>
Low Frequency Wiggle: <wiggle f=0.5>Slow Wiggle</wiggle>

Basic Shake: <shake>Shaking Text</shake>
Violent Shake: <shake a=3>VIOLENT SHAKE</shake>

Wave Effect: <wave>Wavy Text goes up and down</wave>
Fast Wave: <wave f=5>Super Fast Wave</wave>

Rotation ("Rot"): <rot>Rotating Text</rot>
Swing: <swing>Swinging Text</swing>

Rainbow: <rainb>Taste the Rainbow</rainb>
Rainbow (Fast): <rainb f=2>Fast Rainbow</rainb>

Fade In: <fade>Fading In Text</fade>
Delayed Words: Wait for it... <waitfor=1> Now!

<wiggle><rainb>Wiggling Rainbow</rainb></wiggle>
<shake a=0.5><wave f=2>Shaking Wave</wave></shake>

Typewritte Speed: Start normal. <speed=0.1>Slooooooooooow... <speed=10>Faaaaaaaaaaaaaaaast!

Test Complete.
-> END
