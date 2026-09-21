create database Jairo

--tabla empleados--
create table empleados
(
ID  varchar  (10) not null, 
nombres varchar (60),
apellido varchar (60),
telefono varchar (10),
cedula varchar (11),
email varchar (30),
direccion varchar (30),
sexo varchar (60),
puesto varchar (60),
constraint pk_ID_empleados primary key (ID),
)

--tabla cliente--
create table cliente
(
ID int,
nombres varchar (60),
apellido varchar (60),
cedula varchar (11),
telefono varchar (10),
fecha_de_compra date default getdate(),
ID_v varchar(10),
constraint pk_ID primary key (ID),
constraint fk_ID_v foreign key (ID_v) references videojuego(ID_v),
)

--tabla videojuego--
create table videojuego
(
ID_v varchar(10),
Nombre varchar (100),
Genero varchar (100),
Clasificacion varchar (100)
constraint pk_ID_v primary key (ID_v),
Fecha_Lanzamiento date default getdate() 
)

--tabla empresa--
create table empresa
(
ID varchar (10),
nombre varchar (60),
telefono varchar (60),
sucursal varchar (60),
direccion varchar (60),
ID_empleados varchar (10) not null, 
constraint pk_ID primary key (ID),
constraint fk_ID_empleados foreign key (ID) references empleados (ID),
)

insert into empleados(ID,nombres,apellido,telefono,cedula,email,direccion,sexo,puesto) values ('1','Jairo','Matias','8298477528','40212428508','jairo_1829@hotmail.com','calle 3s numero1','masculino','Programador')
insert into empleados(ID,nombres,apellido,telefono,cedula,email,direccion,sexo,puesto) values ('2','Ismael','Paredes','8295549480','40210293045','fabrais159@hotmail.com','calle 44 numero5','Masculino','Diseñador Grafico')
insert into empleados(ID,nombres,apellido,telefono,cedula,email,direccion,sexo,puesto) values ('3','Luis','Nuñez','8295220232','00112298765','nnunez33@gmail.com','calle 4ta','Masculino','Recursos Humanos')
insert into empleados(ID,nombres,apellido,telefono,cedula,email,direccion,sexo,puesto) values ('4','Francis','Rosario','8099183110','00105501826','elfinal08@hotmail.com','Manz 4710 edif 2','Masculino','Relaciones Internacionales')
select * from empleados

insert into cliente(ID,nombres,apellido,cedula,telefono,fecha_de_compra, ID_v) values ('1','Jairo','Matias','40212428508','8298477528','20130226','1')
insert into cliente(ID,nombres,apellido,cedula,telefono,fecha_de_compra, ID_v) values ('2','Ismael','Paredes','40210293045','8295549480','20100727','2')
insert into cliente(ID,nombres,apellido,cedula,telefono,fecha_de_compra, ID_v) values ('3','Luis','Nuñez','00112298765','8295220232','20121105','3')
insert into cliente(ID,nombres,apellido,cedula,telefono,fecha_de_compra, ID_v) values ('4','Francis','Rosario','00105501826','8099183110','20140713','4')
select * from cliente

insert into videojuego (ID_v,nombre,Genero,Clasificacion,Fecha_Lanzamiento) values ('1','Super Smash Bros 4','Peleas','Adolescentes','20141003',)
insert into videojuego (ID_v,nombre,Genero,Clasificacion,Fecha_Lanzamiento) values ('2','Pokemon Sun','Aventura/RPG','Para Todos','20161118')
insert into videojuego (ID_v,nombre,Genero,Clasificacion,Fecha_Lanzamiento) values ('3','Call of Duty Black Ops 3','Tiros','Mayores de 17','20151106')
insert into videojuego (ID_v,nombre,Genero,Clasificacion,Fecha_Lanzamiento) values ('4','GTA 5','Mundo Abierto','Mayores de 17','20131113')
insert into videojuego (ID_v,nombre,Genero,Clasificacion,Fecha_Lanzamiento) values ('5','Need for Speed','Carros','Adolescentes','20151105')
select * from videojuego

drop table empleados
drop table videojuego
drop table cliente
drop table empresa

select * from videojuego where Nombre in ('Super Smash Bros 4','Pokemon Sun')
select * from empleados where Nombres in ('Jairo', 'Ismael')

select cliente.ID, videojuego.genero from cliente inner join videojuego on videojuego.ID_v= cliente.ID_v
select cliente.cedula, videojuego.genero from cliente inner join videojuego on videojuego.ID_v= cliente.ID_v
select cliente.nombres, videojuego.nombre from cliente inner join videojuego on videojuego.ID_V= cliente.ID_v
select cliente.nombres from cliente where nombres='Luis'
