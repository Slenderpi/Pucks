using Pucks;
using Pucks.Level;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class OctaPuckSimulation : PuckSimulation {

	enum EOctaDirection {
		None		= 0,
		Up			= 1,
		UpRight		= 2,
		Right		= 3,
		DownRight	= 4,
		Down		= 5,
		DownLeft	= 6,
		Left		= 7,
		UpLeft		= 8
	}

	public override Vector2Int DragVectorToDirection(Vector3 dragVector) {
		float angle = Vector3.SignedAngle(dragVector, Vector3.up, Vector3.forward);
		if (angle < 0)
			angle += 360;
		return EnumToDirection((EOctaDirection)((angle + 22.5f) % 360 / 45f + 1));
	}

	public override char[,] LevelGridTo2dArray() {
		char[,] grid = new char[WidthCount, HeightCount];
		foreach (var (pos, puck) in StationaryPucks) {
			grid[pos.x, pos.y] = DirectionToChar(puck.Direction);
		}
		foreach (var puck in MovingPucks) {
			grid[puck.GridPoint.x, puck.GridPoint.y] = grid[puck.GridPoint.x, puck.GridPoint.y] switch {
				'^' or 'v' or '<' or '>' or '+' => '+',
				'#' => DirectionToChar(puck.Direction) switch {
					'^' or 'v' => '-',
					'<' or '>' => '|',
					_ => '?'
				},
				'|' => '|',
				'-' => '-',
				_ => DirectionToChar(puck.Direction),
			};
		}
		return grid;
	}

	protected override bool GenerateLevel_Implementation() {
		CurrentLevel.Add(new(4, 4));
		CurrentLevel.Add(new(5, 5));
		CurrentLevel.Add(new(0, 5));
		CurrentLevel.Add(new(5, 0));
		CurrentLevel.Add(new(1, 4));
		CurrentLevel.Add(new(6, 11));
		CurrentLevel.Add(new(6, 5));
		CurrentLevel.Add(new(3, 11));
		CurrentLevel.Add(new(4, 10));
		CurrentLevel.Add(new(7, 15));
		SolutionPosition = new(4, 4);
		SolutionDirection = new(1, 1);
		return true;
	}

	protected override void OnHitStationaryPuck(PuckNode instigator, PuckNode instigated) {
		switch (DirectionToEnum(instigator.Direction)) {
			case EOctaDirection.Up:
				instigator.Direction = EnumToDirection(EOctaDirection.DownRight);
				instigated.Direction = EnumToDirection(EOctaDirection.DownLeft);
				break;
			case EOctaDirection.UpRight:
				instigator.Direction = EnumToDirection(EOctaDirection.Down);
				instigated.Direction = EnumToDirection(EOctaDirection.Left);
				break;
			case EOctaDirection.Right:
				instigator.Direction = EnumToDirection(EOctaDirection.DownLeft);
				instigated.Direction = EnumToDirection(EOctaDirection.UpLeft);
				break;
			case EOctaDirection.DownRight:
				instigator.Direction = EnumToDirection(EOctaDirection.Left);
				instigated.Direction = EnumToDirection(EOctaDirection.Up);
				break;
			case EOctaDirection.Down:
				instigator.Direction = EnumToDirection(EOctaDirection.UpLeft);
				instigated.Direction = EnumToDirection(EOctaDirection.UpRight);
				break;
			case EOctaDirection.DownLeft:
				instigator.Direction = EnumToDirection(EOctaDirection.Up);
				instigated.Direction = EnumToDirection(EOctaDirection.Right);
				break;
			case EOctaDirection.Left:
				instigator.Direction = EnumToDirection(EOctaDirection.UpRight);
				instigated.Direction = EnumToDirection(EOctaDirection.DownRight);
				break;
			case EOctaDirection.UpLeft:
				instigator.Direction = EnumToDirection(EOctaDirection.Right);
				instigated.Direction = EnumToDirection(EOctaDirection.Down);
				break;
		}
		;
	}

	char DirectionToChar(Vector2Int dir) {
		if (dir == Vector2Int.zero)
			return '#';
		if (dir.y == 0) {
			return dir.x < 0 ? '^' : 'v';
		} else {
			return dir.y < 0 ? '<' : '>';
		}
	}

	EOctaDirection DirectionToEnum(Vector2Int direction) => direction.x switch {
		-1 => direction.y switch {
			-1 => EOctaDirection.UpLeft,
			0 => EOctaDirection.Up,
			1 => EOctaDirection.UpRight,
			_ => throw new ArgumentException($"[WeirdQuadPuckSimulation]: DirectionToEnum() was provided an invalid direction: {direction}.")
		},
		0 => direction.y switch {
			-1 => EOctaDirection.Left,
			0 => EOctaDirection.None,
			1 => EOctaDirection.Right,
			_ => throw new ArgumentException($"[WeirdQuadPuckSimulation]: DirectionToEnum() was provided an invalid direction: {direction}.")
		},
		1 => direction.y switch {
			-1 => EOctaDirection.DownLeft,
			0 => EOctaDirection.Down,
			1 => EOctaDirection.DownRight,
			_ => throw new ArgumentException($"[WeirdQuadPuckSimulation]: DirectionToEnum() was provided an invalid direction: {direction}.")
		},
		_ => throw new ArgumentException($"[WeirdQuadPuckSimulation]: DirectionToEnum() was provided an invalid direction: {direction}.")
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Vector2Int EnumToDirection(EOctaDirection dir) => dir switch {
		EOctaDirection.Up => new(-1, 0),
		EOctaDirection.UpRight => new(-1, 1),
		EOctaDirection.Right => new(0, 1),
		EOctaDirection.DownRight => new(1, 1),
		EOctaDirection.Down => new(1, 0),
		EOctaDirection.DownLeft => new(1, -1),
		EOctaDirection.Left => new(0, -1),
		EOctaDirection.UpLeft => new(-1, -1),
		_ => new(0, 0)
	};

}
