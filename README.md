# Cluster Logic Writer
Cluster Logic Writer is a tool working with ClusterVR's **[ ClusterCreatorKit](https://github.com/ClusterVR/ClusterCreatorKit)**, providing ability to edit logics with codes.

![](https://raw.githubusercontent.com/wiki/jd3main/ClusterLogicWriter/example.png)

## Usage

Attach `LogicWriter` to the game object with a logic script (`ItemLogic`, `PlayerLogic`, or `GlobalLogic`), then it will automatically link to the logic script.

Pressing the "Extract" button to convert logic to code.

Pressing the "Compile" button to convert code to logic.

> Errors appear in console after compiling are not harmful most of the time, so you can still build your project.



## Grammar

### Assign Constants

```
i = 1
f = 1.0
b = True
b = true
v2 = Vector2(1, 2)
v3 = Vector3(1, 2, 3)
```

### Basic Operations

```
x = !y
x = -y
x = y + z
x = y - z
x = y * z
x = y / z
x = y % z
x = y && z
x = y || z
x = y ? z : w
```

### Comparison 

```
x = y < z
x = y == z
x = y <= z
```

### Specify Target

```
Global:x = y + z
```

### Signal

```
something <- true
something <- x
something <- x == y
something <- x + y
```

### Other Operations

```
x = Min(y,z)
x = Max(y,z)
```



## Known Issues

* data type of parameters might not be compiled correctly
* there might be errors after compiling (even when compiled correctly)

