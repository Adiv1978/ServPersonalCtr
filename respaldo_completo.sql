--
-- PostgreSQL database dump
--

\restrict DfHi22Ll3l73CiCaPijFCUhEWmttOmmWLE2XWR8WDDYXgmZjiK2ueIltoBxFaDR

-- Dumped from database version 16.11 (Ubuntu 16.11-0ubuntu0.24.04.1)
-- Dumped by pg_dump version 16.11 (Ubuntu 16.11-0ubuntu0.24.04.1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: licencias; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.licencias (
    id integer NOT NULL,
    idpersona integer NOT NULL,
    idusuario integer NOT NULL,
    fecreg timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    feclicenciaini date NOT NULL,
    feclicenciafin date NOT NULL,
    diagnostico text,
    tiempolicencia integer GENERATED ALWAYS AS ((feclicenciafin - feclicenciaini)) STORED,
    nolicencia character varying(50),
    auditoria boolean DEFAULT false,
    observacion text
);


ALTER TABLE public.licencias OWNER TO postgres;

--
-- Name: personal; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.personal (
    id integer NOT NULL,
    cedula character varying(20) NOT NULL,
    nombre character varying(100) NOT NULL,
    apellidos character varying(100) NOT NULL,
    puestotrabajo character varying(100)
);


ALTER TABLE public.personal OWNER TO postgres;

--
-- Name: tbuser; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tbuser (
    id integer NOT NULL,
    nick character varying(50) NOT NULL,
    pass character varying(50) NOT NULL,
    rolusuario smallint NOT NULL,
    activo boolean DEFAULT true,
    CONSTRAINT tbuser_rolusuario_check CHECK ((rolusuario = ANY (ARRAY[1, 2, 3])))
);


ALTER TABLE public.tbuser OWNER TO postgres;

--
-- Name: vw_licencias; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.vw_licencias AS
 SELECT l.id AS licencia_id,
    l.nolicencia,
    l.idpersona,
    p.cedula AS empleado_cedula,
    (((p.nombre)::text || ' '::text) || (p.apellidos)::text) AS empleado_nombre_completo,
    p.puestotrabajo,
    l.feclicenciaini,
    l.feclicenciafin,
    l.tiempolicencia,
    l.diagnostico,
    l.observacion,
    l.auditoria,
    l.fecreg AS fecha_registro_sistema,
    l.idusuario AS registrado_por_id,
    u.nick AS registrado_por_nick
   FROM ((public.licencias l
     JOIN public.personal p ON ((l.idpersona = p.id)))
     JOIN public.tbuser u ON ((l.idusuario = u.id)));


ALTER VIEW public.vw_licencias OWNER TO postgres;

--
-- Name: fn_getlicencia(text, integer, integer, date, date, timestamp without time zone, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_getlicencia(p_tokenid text, p_minutoscaducaseccion integer, p_idpersona integer DEFAULT 0, p_fecinicio date DEFAULT NULL::date, p_fecfin date DEFAULT NULL::date, p_fecregdesde timestamp without time zone DEFAULT NULL::timestamp without time zone, p_fecreghasta timestamp without time zone DEFAULT NULL::timestamp without time zone) RETURNS SETOF public.vw_licencias
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- 1. Validar sesión (Rol 4: Acceso para cualquier usuario autenticado)
    PERFORM fn_validateseccion(p_tokenid, p_minutoscaducaseccion, 4::SMALLINT);

    -- 2. Retornar los registros aplicando filtros solo si los parámetros no son nulos/vacíos
    RETURN QUERY 
    SELECT *
    FROM vw_licencias v
    WHERE 
        -- Filtro por ID de persona
        (p_idpersona IS NULL OR p_idpersona = 0 OR v.idpersona = p_idpersona)
        
        -- Lógica de cobertura: La licencia debe abarcar el periodo solicitado
        AND (p_fecinicio IS NULL OR v.feclicenciaini >= p_fecinicio)
        AND (p_fecfin IS NULL OR v.feclicenciafin <= p_fecfin)
        
        -- Filtro por fecha de creación en el sistema
        AND (p_fecregdesde IS NULL OR v.fecha_registro_sistema >= p_fecregdesde)
        AND (p_fecreghasta IS NULL OR v.fecha_registro_sistema <= p_fecreghasta)
        
    ORDER BY v.fecha_registro_sistema DESC;
END;
$$;


ALTER FUNCTION public.fn_getlicencia(p_tokenid text, p_minutoscaducaseccion integer, p_idpersona integer, p_fecinicio date, p_fecfin date, p_fecregdesde timestamp without time zone, p_fecreghasta timestamp without time zone) OWNER TO postgres;

--
-- Name: vw_personal; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.vw_personal AS
 SELECT id,
    cedula,
    nombre,
    apellidos,
    (((nombre)::text || ' '::text) || (apellidos)::text) AS nombre_completo,
    puestotrabajo
   FROM public.personal;


ALTER VIEW public.vw_personal OWNER TO postgres;

--
-- Name: fn_getpersona(text, integer, integer, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_getpersona(p_tokenid text, p_minutoscaducaseccion integer, p_id integer DEFAULT 0, p_busqueda character varying DEFAULT ''::character varying) RETURNS SETOF public.vw_personal
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM fn_validateseccion(p_tokenid, p_minutoscaducaseccion, 4::SMALLINT);
    RETURN QUERY 
    SELECT *
    FROM vw_personal v
    WHERE 
        (p_id IS NULL OR p_id = 0 OR v.id = p_id)
        AND (
            p_busqueda IS NULL OR p_busqueda = '' OR 
            v.cedula ILIKE '%' || p_busqueda || '%' OR 
            v.nombre_completo ILIKE '%' || p_busqueda || '%'
        )
    ORDER BY v.nombre_completo ASC;
END;
$$;


ALTER FUNCTION public.fn_getpersona(p_tokenid text, p_minutoscaducaseccion integer, p_id integer, p_busqueda character varying) OWNER TO postgres;

--
-- Name: fn_setlicencias(text, integer, integer, date, date, text, character varying, boolean, text); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_setlicencias(p_tokenid text, p_minutoscaducaseccion integer, p_idpersona integer, p_feclicenciaini date, p_feclicenciafin date, p_diagnostico text, p_nolicencia character varying, p_auditoria boolean, p_observacion text) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_result INT;
    v_idusuario INT;
BEGIN
    -- 1. Validar sesión (Nivel 2: Operador o Admin) y obtener el ID del usuario actual
    SELECT UsuarioId INTO v_idusuario
    FROM fn_validateseccion(p_tokenid, p_minutoscaducaseccion, 2::SMALLINT);

    -- 2. Validaciones de integridad
    IF p_feclicenciafin < p_feclicenciaini THEN
        RAISE EXCEPTION 'Error: La fecha de fin es anterior a la de inicio.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM Personal WHERE id = p_idpersona) THEN
        RAISE EXCEPTION 'Error: El empleado con ID % no existe.', p_idpersona;
    END IF;

    -- 4. Inserción pura (El ID se autogenera por el tipo SERIAL de la tabla)
    INSERT INTO Licencias (
        idpersona, 
        idusuario, 
        feclicenciaini, 
        feclicenciafin, 
        diagnostico, 
        nolicencia, 
        auditoria, 
        observacion
    )
    VALUES (
        p_idpersona, 
        v_idusuario, 
        p_feclicenciaini, 
        p_feclicenciafin, 
        p_diagnostico, 
        p_nolicencia, 
        COALESCE(p_auditoria, FALSE), 
        p_observacion
    )
    RETURNING id INTO v_id_result;

    -- 5. Retornar el nuevo ID generado
    RETURN v_id_result;
END;
$$;


ALTER FUNCTION public.fn_setlicencias(p_tokenid text, p_minutoscaducaseccion integer, p_idpersona integer, p_feclicenciaini date, p_feclicenciafin date, p_diagnostico text, p_nolicencia character varying, p_auditoria boolean, p_observacion text) OWNER TO postgres;

--
-- Name: fn_setpersonal(text, integer, integer, character varying, character varying, character varying, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_setpersonal(p_tokenid text, p_minutoscaducaseccion integer, p_id integer, p_cedula character varying, p_nombre character varying, p_apellidos character varying, p_puestotrabajo character varying) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_result INT;
BEGIN
    PERFORM fn_validateseccion(p_tokenid, p_minutoscaducaseccion, 1::SMALLINT);
    IF p_id IS NULL OR p_id = 0 THEN
        IF EXISTS (SELECT 1 FROM Personal WHERE cedula = p_cedula) THEN
            RAISE EXCEPTION 'La cédula (%) ya está registrada en el sistema.', p_cedula;
        END IF;
        INSERT INTO Personal (cedula, nombre, apellidos, puestotrabajo)
        VALUES (p_cedula, p_nombre, p_apellidos, p_puestotrabajo)
        RETURNING id INTO v_id_result;
    ELSE
        IF EXISTS (SELECT 1 FROM Personal WHERE cedula = p_cedula AND id != p_id) THEN
            RAISE EXCEPTION 'La cédula (%) ya pertenece a otro registro de personal.', p_cedula;
        END IF;
        UPDATE Personal
        SET 
		    puestotrabajo = p_puestotrabajo
        WHERE id = p_id
        RETURNING id INTO v_id_result;
        IF v_id_result IS NULL THEN
            RAISE EXCEPTION 'El registro de personal que intenta actualizar no existe.';
        END IF;
    END IF;
    RETURN v_id_result;
END;
$$;


ALTER FUNCTION public.fn_setpersonal(p_tokenid text, p_minutoscaducaseccion integer, p_id integer, p_cedula character varying, p_nombre character varying, p_apellidos character varying, p_puestotrabajo character varying) OWNER TO postgres;

--
-- Name: fn_setseccion(character varying, character varying, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_setseccion(p_nick character varying, p_pass character varying, p_minutoscaducaseccion integer) RETURNS TABLE(token text, usuarioid integer, rol smallint)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_idusuario INT;
    v_rol SMALLINT;
    v_token TEXT;
BEGIN
    SELECT Id, rolusuario INTO v_idusuario, v_rol
    FROM TBUser
    WHERE Nick = p_nick AND Pass = p_pass AND activo = true;
    IF v_idusuario IS NULL THEN
        RAISE EXCEPTION 'Credenciales incorrectas o usuario inactivo.';
    END IF;
    DELETE FROM TBSeccion WHERE idusuario = v_idusuario;
    v_token := gen_random_uuid()::text;
    INSERT INTO TBSeccion (idusuario, tokenid, fecinicio, fecexpiracion, isactivo)
    VALUES (
        v_idusuario,
        v_token,
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP + (p_minutoscaducaseccion * INTERVAL '1 minute'),
        true
    );
    RETURN QUERY SELECT v_token, v_idusuario, v_rol;
END;
$$;


ALTER FUNCTION public.fn_setseccion(p_nick character varying, p_pass character varying, p_minutoscaducaseccion integer) OWNER TO postgres;

--
-- Name: fn_setuser(text, integer, integer, character varying, character varying, smallint, boolean); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_setuser(p_tokenid text, p_minutoscaducaseccion integer, p_id integer, p_nick character varying, p_pass character varying, p_rolusuario smallint, p_activo boolean DEFAULT true) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_result INT;
BEGIN
    PERFORM fn_validateseccion(p_tokenid, p_minutoscaducaseccion, 1::SMALLINT);
    IF p_rolusuario NOT IN (1, 2, 3) THEN
        RAISE EXCEPTION 'Rol inválido. Debe ser 1 (Admin), 2 (Operador) o 3 (Consultas).';
    END IF;
    IF p_id IS NULL OR p_id = 0 THEN
        IF EXISTS (SELECT 1 FROM TBUser WHERE Nick = p_nick) THEN
            RAISE EXCEPTION 'El nombre de usuario (%) ya está registrado.', p_nick;
        END IF;
        INSERT INTO TBUser (Nick, Pass, rolusuario, activo)
        VALUES (p_nick, p_pass, p_rolusuario, p_activo)
        RETURNING Id INTO v_id_result;
    ELSE
        IF EXISTS (SELECT 1 FROM TBUser WHERE Nick = p_nick AND Id != p_id) THEN
            RAISE EXCEPTION 'El nombre de usuario (%) ya está siendo usado por otra persona.', p_nick;
        END IF;
        UPDATE TBUser
        SET Nick = p_nick,
            Pass = p_pass,
            rolusuario = p_rolusuario,
            activo = p_activo
        WHERE Id = p_id
        RETURNING Id INTO v_id_result;
        IF v_id_result IS NULL THEN
            RAISE EXCEPTION 'El usuario que intenta actualizar no existe.';
        END IF;
   END IF;
    RETURN v_id_result;
END;
$$;


ALTER FUNCTION public.fn_setuser(p_tokenid text, p_minutoscaducaseccion integer, p_id integer, p_nick character varying, p_pass character varying, p_rolusuario smallint, p_activo boolean) OWNER TO postgres;

--
-- Name: fn_updatepassworduser(text, integer, character varying, character varying, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_updatepassworduser(p_tokenid text, p_minutoscaducaseccion integer, p_nick character varying, p_pass_actual character varying, p_pass_nuevo character varying) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_ejecutor INT;
    v_id_validado INT;
BEGIN
    SELECT UsuarioId INTO v_id_ejecutor
    FROM fn_validateseccion(p_tokenid, p_minutoscaducaseccion, 4::SMALLINT);
    SELECT Id INTO v_id_validado
    FROM TBUser
    WHERE Id = v_id_ejecutor AND Nick = p_nick AND Pass = p_pass_actual;
    IF v_id_validado IS NULL THEN
        RAISE EXCEPTION 'Credenciales incorrectas. Verifique su Nick y su contraseña actual.';
    END IF;
    UPDATE TBUser
    SET Pass = p_pass_nuevo
    WHERE Id = v_id_ejecutor;
    RETURN TRUE;
END;
$$;


ALTER FUNCTION public.fn_updatepassworduser(p_tokenid text, p_minutoscaducaseccion integer, p_nick character varying, p_pass_actual character varying, p_pass_nuevo character varying) OWNER TO postgres;

--
-- Name: fn_updatepersonal(text, integer, integer, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_updatepersonal(p_tokenid text, p_minutoscaducaseccion integer, p_id integer, p_puestotrabajo character varying) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM fn_validateseccion(p_tokenid, p_minutoscaducaseccion, 1::SMALLINT);
    IF NOT EXISTS (SELECT 1 FROM Personal WHERE id = p_id) THEN
        RAISE EXCEPTION 'El registro de personal que intenta actualizar no existe.';
    END IF;
    UPDATE Personal
    SET puestotrabajo = p_puestotrabajo
    WHERE id = p_id;
    RETURN TRUE;
END;
$$;


ALTER FUNCTION public.fn_updatepersonal(p_tokenid text, p_minutoscaducaseccion integer, p_id integer, p_puestotrabajo character varying) OWNER TO postgres;

--
-- Name: fn_validateseccion(text, integer, smallint); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_validateseccion(p_tokenid text, p_minutoscaducaseccion integer, p_rollevel smallint) RETURNS TABLE(usuarioid integer, rol smallint)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_idusuario INT;
    v_rol SMALLINT;
BEGIN
    DELETE FROM TBSeccion WHERE fecexpiracion < CURRENT_TIMESTAMP;
    SELECT s.idusuario, u.rolusuario
    INTO v_idusuario, v_rol
    FROM TBSeccion s
    INNER JOIN TBUser u ON s.idusuario = u.Id
    WHERE s.tokenid = p_tokenid AND s.isactivo = true AND u.activo = true;
    IF v_idusuario IS NULL THEN
        RAISE EXCEPTION 'Token inválido, sesión caducada o usuario inactivo.';
    END IF;
    IF v_rol != 1 AND p_rollevel != 4 AND v_rol != p_rollevel THEN
        RAISE EXCEPTION 'Acceso denegado: No tiene los permisos necesarios.';
    END IF;
    UPDATE TBSeccion
    SET fecexpiracion = CURRENT_TIMESTAMP + (p_minutoscaducaseccion * INTERVAL '1 minute')
    WHERE tokenid = p_tokenid;
    RETURN QUERY SELECT v_idusuario, v_rol;
END;
$$;


ALTER FUNCTION public.fn_validateseccion(p_tokenid text, p_minutoscaducaseccion integer, p_rollevel smallint) OWNER TO postgres;

--
-- Name: licencias_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.licencias_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.licencias_id_seq OWNER TO postgres;

--
-- Name: licencias_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.licencias_id_seq OWNED BY public.licencias.id;


--
-- Name: personal_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.personal_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.personal_id_seq OWNER TO postgres;

--
-- Name: personal_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.personal_id_seq OWNED BY public.personal.id;


--
-- Name: tbseccion; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tbseccion (
    id integer NOT NULL,
    idusuario integer NOT NULL,
    tokenid text NOT NULL,
    fecinicio timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecexpiracion timestamp without time zone NOT NULL,
    isactivo boolean DEFAULT true
);


ALTER TABLE public.tbseccion OWNER TO postgres;

--
-- Name: tbseccion_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.tbseccion_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.tbseccion_id_seq OWNER TO postgres;

--
-- Name: tbseccion_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.tbseccion_id_seq OWNED BY public.tbseccion.id;


--
-- Name: tbuser_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.tbuser_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.tbuser_id_seq OWNER TO postgres;

--
-- Name: tbuser_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.tbuser_id_seq OWNED BY public.tbuser.id;


--
-- Name: licencias id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.licencias ALTER COLUMN id SET DEFAULT nextval('public.licencias_id_seq'::regclass);


--
-- Name: personal id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.personal ALTER COLUMN id SET DEFAULT nextval('public.personal_id_seq'::regclass);


--
-- Name: tbseccion id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tbseccion ALTER COLUMN id SET DEFAULT nextval('public.tbseccion_id_seq'::regclass);


--
-- Name: tbuser id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tbuser ALTER COLUMN id SET DEFAULT nextval('public.tbuser_id_seq'::regclass);


--
-- Data for Name: licencias; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.licencias (id, idpersona, idusuario, fecreg, feclicenciaini, feclicenciafin, diagnostico, nolicencia, auditoria, observacion) FROM stdin;
1	2	1	2026-02-22 23:37:11.968403	2026-02-17	2026-02-22	asdasd	1	f	dadsasddsa
2	1	1	2026-02-23 02:56:43.069781	2026-02-22	2026-02-27	rrettre	1	f	ertetrerrtt
3	1	1	2026-02-24 23:37:11.868688	2026-02-17	2026-02-22	wdqihwdohwdqohwdq	1	t	spdspjsadpoj\nwdoihwohwoi
\.


--
-- Data for Name: personal; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.personal (id, cedula, nombre, apellidos, puestotrabajo) FROM stdin;
1	049-0049375-2	Ramon Amable	Gonzalez Peguero	Prueba
2	023-0025365-5	Eustaquio	Ppp	Prueba
\.


--
-- Data for Name: tbseccion; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.tbseccion (id, idusuario, tokenid, fecinicio, fecexpiracion, isactivo) FROM stdin;
18	1	7037294f-d7a7-4e39-a528-4619f91fbba9	2026-02-25 14:48:27.522587	2026-02-25 16:38:28.544355	t
\.


--
-- Data for Name: tbuser; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.tbuser (id, nick, pass, rolusuario, activo) FROM stdin;
1	Administrador	qaz789	1	t
\.


--
-- Name: licencias_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.licencias_id_seq', 3, true);


--
-- Name: personal_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.personal_id_seq', 2, true);


--
-- Name: tbseccion_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.tbseccion_id_seq', 18, true);


--
-- Name: tbuser_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.tbuser_id_seq', 1, true);


--
-- Name: licencias licencias_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.licencias
    ADD CONSTRAINT licencias_pkey PRIMARY KEY (id);


--
-- Name: personal personal_cedula_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.personal
    ADD CONSTRAINT personal_cedula_key UNIQUE (cedula);


--
-- Name: personal personal_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.personal
    ADD CONSTRAINT personal_pkey PRIMARY KEY (id);


--
-- Name: tbseccion tbseccion_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tbseccion
    ADD CONSTRAINT tbseccion_pkey PRIMARY KEY (id);


--
-- Name: tbuser tbuser_nick_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tbuser
    ADD CONSTRAINT tbuser_nick_key UNIQUE (nick);


--
-- Name: tbuser tbuser_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tbuser
    ADD CONSTRAINT tbuser_pkey PRIMARY KEY (id);


--
-- Name: licencias fk_licencias_personal; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.licencias
    ADD CONSTRAINT fk_licencias_personal FOREIGN KEY (idpersona) REFERENCES public.personal(id) ON DELETE CASCADE;


--
-- Name: licencias fk_licencias_usuario; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.licencias
    ADD CONSTRAINT fk_licencias_usuario FOREIGN KEY (idusuario) REFERENCES public.tbuser(id) ON DELETE RESTRICT;


--
-- Name: tbseccion fk_seccion_usuario; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tbseccion
    ADD CONSTRAINT fk_seccion_usuario FOREIGN KEY (idusuario) REFERENCES public.tbuser(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict DfHi22Ll3l73CiCaPijFCUhEWmttOmmWLE2XWR8WDDYXgmZjiK2ueIltoBxFaDR

