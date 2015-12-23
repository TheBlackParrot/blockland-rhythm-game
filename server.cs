if(!isObject(RhythmReader)) {
	new FileObject(RhythmReader);
}

function loadSong(%song) {
	alxStopAll();

	%filename = "Add-Ons/Gamemode_Rhythm/songs/" @ %song;

	if(!isFile(%filename @ ".blr")) {
		messageAll('', "\c0ERROR: \c6Song\c3" SPC %song SPC "\c6not found!");
		serverPlay2D(errorSound);
		return;
	}

	RhythmReader.openForRead(%filename @ ".blr");

	$Rhythm::Title = RhythmReader.readLine();
	$Rhythm::Artist = RhythmReader.readLine();
	$Rhythm::Tempo = RhythmReader.readLine();

	talk($Rhythm::Artist SPC "-" SPC $Rhythm::Title);
	talk($Rhythm::Tempo SPC "BPM (event delay:" SPC 60000/$Rhythm::Tempo @ "ms)");

	$Rhythm::NoteCount = 0;

	// multiplayer servers would throw a fit here
	// luckily this is single-player-only, so we can abuse the engine more
	eval("datablock AudioProfile(musicData_Rhythm) {fileName = \"" @ %filename @ ".ogg\"; description = \"AudioMusicLooping3d\"; preload = 1; uiName = \"" @ %song @ "\";};");

	%delay = (60000/$Rhythm::Tempo)/4;
	if(isObject($Rhythm::Audio)) {
		$Rhythm::Audio.delete();
	}
	schedule(%delay*27+(ClientGroup.getObject(0).getPing()), 0, playMusic);
	_tempo.schedule(%delay*27+(ClientGroup.getObject(0).getPing()), tick);
	doReading();
}

function playMusic() {
	$Rhythm::Audio = new AudioEmitter() {
		is3D = 0;
		profile = "musicData_Rhythm";
		referenceDistance = 999999;
		maxDistance = 999999;
		volume = 5;
		position = ClientGroup.getObject(0).player.getPosition();
	};
}

function fxDTSBrick::tick(%this) {
	if($Rhythm::Stop) {
		return;
	}

	%this.setColorFX(3);
	%this.schedule((60000/$Rhythm::Tempo)/2, setColorFX, 0);

	serverPlay2D(brickPlantSound);

	%this.schedule((60000/$Rhythm::Tempo), tick);
}

function doReading() {
	%delay = (60000/$Rhythm::Tempo)/4;

	%line = RhythmReader.readLine();
	while(getSubStr(%line, 0, 2) $= "//") {
		%line = RhythmReader.readLine();
	}

	%direction = "left down up right";
	for(%i=0;%i<4;%i++) {
		if(getSubStr(%line, %i, 1)) {
			sendNote(getWord(%direction, %i));
		}
	}

	if(RhythmReader.isEOF()) {
		RhythmReader.close();
		return;
	}
	schedule(%delay, 0, doReading);
}

function sendNote(%direction) {
	$Rhythm::Note[$Rhythm::NoteCount] = 0;
	$Rhythm::NoteCount++;

	%delay = (60000/$Rhythm::Tempo)/4;
	digNote(1, %delay, %direction);
}

function digNote(%current, %delay, %direction) {
	if(%current == 28) {
		%brick = "_" @ %direction @ "_hit";
		if(%brick.colorFxID == 3) {
			serverPlay2D(brickRotateSound);
		} else {
			serverPlay2D(errorSound);
		}
		return;
	}

	%brick = "_" @ %direction @ "_" @ %current;
	
	%brick.toggleNote(1);
	%brick.schedule(%delay, toggleNote, 0);

	schedule(%delay, 0, digNote, %current++, %delay, %direction);
}

//function fxDTSBrick::checkIfPressed(%this)

function fxDTSBrick::toggleNote(%this, %toggle) {
	if(%toggle) {
		%this.setColorFX(3);
	} else {
		%this.setColorFX(0);
	}
}

package RhythmTest {
	function moveLeft(%trigger) {
		switch(%trigger) {
			case 0:
				_left_hit.setColorFX(0);
			case 1:
				_left_hit.setColorFX(3);
		}
		if($Rhythm::AllowMovement) {
			return parent::moveLeft(%trigger);
		}
	}

	function moveRight(%trigger) {
		switch(%trigger) {
			case 0:
				_right_hit.setColorFX(0);
			case 1:
				_right_hit.setColorFX(3);
		}
		if($Rhythm::AllowMovement) {
			return parent::moveRight(%trigger);
		}
	}

	function moveForward(%trigger) {
		switch(%trigger) {
			case 0:
				_up_hit.setColorFX(0);
			case 1:
				_up_hit.setColorFX(3);
		}
		if($Rhythm::AllowMovement) {
			return parent::moveForward(%trigger);
		}
	}

	function moveBackward(%trigger) {
		switch(%trigger) {
			case 0:
				_down_hit.setColorFX(0);
			case 1:
				_down_hit.setColorFX(3);
		}
		if($Rhythm::AllowMovement) {
			return parent::moveBackward(%trigger);
		}
	}
};
activatePackage(RhythmTest);