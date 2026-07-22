--
-- PostgreSQL database dump
--

\restrict 7VviKafUg364ZfwXQ4B2LidmVT40QV1QIyE4TfLcjG9OpyDCWHZo7rFEdTGh5we

-- Dumped from database version 17.9 (Debian 17.9-1.pgdg13+1)
-- Dumped by pg_dump version 17.9 (Debian 17.9-1.pgdg13+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: pg_trgm; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;


--
-- Name: EXTENSION pg_trgm; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION pg_trgm IS 'text similarity measurement and index searching based on trigrams';


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
    u.nick AS registrado_por_nick,
    (l.feclicenciafin - CURRENT_DATE) AS diafaltantes
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
-- Name: fn_getlicenciaini(date, date); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_getlicenciaini(p_fecini_a date, p_fecini_b date) RETURNS SETOF public.vw_licencias
    LANGUAGE sql STABLE
    AS $$
    SELECT v.*
    FROM public.vw_licencias AS v
    WHERE v.feclicenciaini >= p_fecini_a
      AND v.feclicenciaini < p_fecini_b;
$$;


ALTER FUNCTION public.fn_getlicenciaini(p_fecini_a date, p_fecini_b date) OWNER TO postgres;

--
-- Name: FUNCTION fn_getlicenciaini(p_fecini_a date, p_fecini_b date); Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON FUNCTION public.fn_getlicenciaini(p_fecini_a date, p_fecini_b date) IS 'Retorna licencias cuya fecha de inicio pertenece al intervalo [p_fecini_a, p_fecini_b).';


--
-- Name: vw_licenciasvigentes; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.vw_licenciasvigentes AS
 SELECT licencia_id,
    nolicencia,
    idpersona,
    empleado_cedula,
    empleado_nombre_completo,
    puestotrabajo,
    feclicenciaini,
    feclicenciafin,
    tiempolicencia,
    diagnostico,
    observacion,
    auditoria,
    fecha_registro_sistema,
    registrado_por_id,
    registrado_por_nick,
    diafaltantes
   FROM public.vw_licencias
  WHERE (feclicenciafin >= CURRENT_DATE);


ALTER VIEW public.vw_licenciasvigentes OWNER TO postgres;

--
-- Name: fn_getlicenciasactivas(integer, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_getlicenciasactivas(p_numero_pagina integer, p_registros_por_pagina integer) RETURNS SETOF public.vw_licenciasvigentes
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_offset integer;
BEGIN
    -- Validación básica de seguridad para evitar páginas negativas o cero
    IF p_numero_pagina < 1 THEN
        p_numero_pagina := 1;
    END IF;

    -- Calcular cuántos registros saltar basado en la página actual
    v_offset := (p_numero_pagina - 1) * p_registros_por_pagina;

    RETURN QUERY 
    SELECT * 
    FROM public.vw_licenciasvigentes
    ORDER BY feclicenciafin ASC, licencia_id ASC -- Ordenamos por las que vencen pronto, y luego por ID
    LIMIT p_registros_por_pagina 
    OFFSET v_offset;
END;
$$;


ALTER FUNCTION public.fn_getlicenciasactivas(p_numero_pagina integer, p_registros_por_pagina integer) OWNER TO postgres;

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
            -- Remueve los guiones de la columna y del parámetro para comparar solo los números
            REPLACE(v.cedula, '-', '') ILIKE '%' || REPLACE(p_busqueda, '-', '') || '%' OR 
            v.nombre_completo ILIKE '%' || p_busqueda || '%'
        )
    ORDER BY v.nombre_completo ASC;
END;
$$;


ALTER FUNCTION public.fn_getpersona(p_tokenid text, p_minutoscaducaseccion integer, p_id integer, p_busqueda character varying) OWNER TO postgres;

--
-- Name: fn_getsoportedoc(text, integer, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_getsoportedoc(p_tokenid text, p_minutoscaducaseccion integer, p_idlicencia integer) RETURNS TABLE(id integer, idlicencia integer, fecreg timestamp without time zone, nombrearchivo character varying, hashfile text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- 1. Validar sesión.
    -- Rol 4: Acceso para cualquier usuario autenticado.
    PERFORM public.fn_validateseccion(
        p_tokenid,
        p_minutoscaducaseccion,
        4::smallint
    );

    -- 2. Validar ID de licencia.
    IF p_idlicencia IS NULL OR p_idlicencia <= 0 THEN
        RAISE EXCEPTION 'Error: Debe especificar un ID de licencia válido.';
    END IF;

    -- 3. Retornar documentos de soporte asociados a la licencia.
    RETURN QUERY
    SELECT 
        s.id,
        s.idlicencia,
        s.fecreg,
        s.nombrearchivo,
        s.hashfile
    FROM public.soportedoc s
    WHERE s.idlicencia = p_idlicencia
    ORDER BY s.fecreg DESC, s.id DESC;
END;
$$;


ALTER FUNCTION public.fn_getsoportedoc(p_tokenid text, p_minutoscaducaseccion integer, p_idlicencia integer) OWNER TO postgres;

--
-- Name: fn_setlicencias(text, integer, integer, date, date, text, character varying, boolean, text, jsonb); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_setlicencias(p_tokenid text, p_minutoscaducaseccion integer, p_idpersona integer, p_feclicenciaini date, p_feclicenciafin date, p_diagnostico text, p_nolicencia character varying, p_auditoria boolean, p_observacion text, p_soportes jsonb DEFAULT '[]'::jsonb) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_result integer;
    v_idusuario integer;

    v_anio_actual integer;
    v_ultimo_numero integer;
    v_nuevo_numero integer;
    v_nolicencia character varying(50);

    v_archivo jsonb;
    v_nombrearchivo character varying(50);
    v_hashfile text;
    
    v_existe_solapamiento boolean;
BEGIN
    -- 1. Validar sesión y obtener el ID del usuario actual.
    SELECT usuarioid
    INTO v_idusuario
    FROM public.fn_validateseccion(
        p_tokenid,
        p_minutoscaducaseccion,
        2::smallint
    );

    -- 2. Validaciones de integridad.
    IF p_feclicenciaini IS NULL THEN
        RAISE EXCEPTION 'Error: La fecha de inicio de licencia es obligatoria.';
    END IF;

    IF p_feclicenciafin IS NULL THEN
        RAISE EXCEPTION 'Error: La fecha de fin de licencia es obligatoria.';
    END IF;

    IF p_feclicenciafin < p_feclicenciaini THEN
        RAISE EXCEPTION 'Error: La fecha de fin es anterior a la de inicio.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM public.personal
        WHERE id = p_idpersona
    ) THEN
        RAISE EXCEPTION 'Error: El empleado con ID % no existe.', p_idpersona;
    END IF;

    -- NUEVA VALIDACIÓN: Verificar si las fechas se solapan con una licencia existente
    SELECT EXISTS (
        SELECT 1 
        FROM public.licencias
        WHERE idpersona = p_idpersona
          AND (feclicenciaini, feclicenciafin) OVERLAPS (p_feclicenciaini, p_feclicenciafin)
    ) INTO v_existe_solapamiento;

    IF v_existe_solapamiento THEN
        RAISE EXCEPTION 'Error: El empleado ya tiene una licencia registrada que coincide o se cruza con el rango solicitado (% al %).', 
            p_feclicenciaini, p_feclicenciafin;
    END IF;

    -- 3. Validar que p_soportes sea un arreglo JSON.
    IF p_soportes IS NULL THEN
        p_soportes := '[]'::jsonb;
    END IF;

    IF jsonb_typeof(p_soportes) <> 'array' THEN
        RAISE EXCEPTION 'Error: p_soportes debe ser un arreglo JSON.';
    END IF;

    -- 4. Obtener el año actual.
    v_anio_actual := EXTRACT(YEAR FROM CURRENT_DATE)::integer;

    -- 5. Buscar el último número de licencia de esa persona en el año actual.
    SELECT COALESCE(
        MAX(
            NULLIF(
                split_part(nolicencia, '-', 3),
                ''
            )::integer
        ),
        0
    )
    INTO v_ultimo_numero
    FROM public.licencias
    WHERE idpersona = p_idpersona
      AND EXTRACT(YEAR FROM feclicenciaini)::integer = v_anio_actual
      AND nolicencia LIKE 'HVM-' || v_anio_actual || '-%';

    -- 6. Sumar 1 al último número encontrado.
    v_nuevo_numero := v_ultimo_numero + 1;

    -- 7. Generar el nuevo número de licencia.
    v_nolicencia := 'HVM-'
                    || v_anio_actual
                    || '-'
                    || LPAD(v_nuevo_numero::text, 2, '0');

    -- 8. Insertar la licencia.
    INSERT INTO public.licencias (
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
        v_nolicencia,
        COALESCE(p_auditoria, false),
        p_observacion
    )
    RETURNING id INTO v_id_result;

    -- 9. Insertar documentos soporte asociados a la licencia.
    FOR v_archivo IN
        SELECT value
        FROM jsonb_array_elements(p_soportes)
    LOOP
        v_nombrearchivo := NULLIF(trim(v_archivo ->> 'nombrearchivo'), '');
        v_hashfile := NULLIF(trim(v_archivo ->> 'hashfile'), '');

        IF v_nombrearchivo IS NULL THEN
            RAISE EXCEPTION 'Error: Cada soporte debe tener nombrearchivo.';
        END IF;

        IF v_hashfile IS NULL THEN
            RAISE EXCEPTION 'Error: Cada soporte debe tener hashfile.';
        END IF;

        INSERT INTO public.soportedoc (
            idlicencia,
            fecreg,
            nombrearchivo,
            hashfile
        )
        VALUES (
            v_id_result,
            CURRENT_TIMESTAMP,
            v_nombrearchivo,
            v_hashfile
        );
    END LOOP;

    -- 10. Retornar el ID de la licencia generada.
    RETURN v_id_result;
END;
$$;


ALTER FUNCTION public.fn_setlicencias(p_tokenid text, p_minutoscaducaseccion integer, p_idpersona integer, p_feclicenciaini date, p_feclicenciafin date, p_diagnostico text, p_nolicencia character varying, p_auditoria boolean, p_observacion text, p_soportes jsonb) OWNER TO postgres;

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
-- Name: fn_updatelicencia(text, integer, integer, date, date, text, boolean, text); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_updatelicencia(p_tokenid text, p_minutoscaducaseccion integer, p_idlicencia integer, p_feclicenciaini date, p_feclicenciafin date, p_diagnostico text, p_auditoria boolean, p_observacion text) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_idusuario integer;
    v_existe integer;
BEGIN
    -- 1. Validar sesión.
    -- Rol 2: operador.
    -- El rol 1 administrador pasa por la lógica actual de fn_validateseccion.
    SELECT usuarioid
    INTO v_idusuario
    FROM public.fn_validateseccion(
        p_tokenid,
        p_minutoscaducaseccion,
        2::smallint
    );

    -- 2. Validar ID de licencia.
    IF p_idlicencia IS NULL OR p_idlicencia <= 0 THEN
        RAISE EXCEPTION 'Error: Debe especificar un ID de licencia válido.';
    END IF;

    SELECT COUNT(*)
    INTO v_existe
    FROM public.licencias
    WHERE id = p_idlicencia;

    IF v_existe = 0 THEN
        RAISE EXCEPTION 'Error: La licencia con ID % no existe.', p_idlicencia;
    END IF;

    -- 3. Validar fechas.
    IF p_feclicenciaini IS NULL THEN
        RAISE EXCEPTION 'Error: La fecha de inicio de licencia es obligatoria.';
    END IF;

    IF p_feclicenciafin IS NULL THEN
        RAISE EXCEPTION 'Error: La fecha final de licencia es obligatoria.';
    END IF;

    IF p_feclicenciafin < p_feclicenciaini THEN
        RAISE EXCEPTION 'Error: La fecha de fin no puede ser anterior a la fecha de inicio.';
    END IF;

    -- 4. Actualizar licencia.
    -- No se actualiza nolicencia.
    -- No se actualiza tiempolicencia porque PostgreSQL lo recalcula automáticamente.
    UPDATE public.licencias
    SET feclicenciaini = p_feclicenciaini,
        feclicenciafin = p_feclicenciafin,
        diagnostico = p_diagnostico,
        auditoria = COALESCE(p_auditoria, false),
        observacion = p_observacion
    WHERE id = p_idlicencia;

    RETURN true;
END;
$$;


ALTER FUNCTION public.fn_updatelicencia(p_tokenid text, p_minutoscaducaseccion integer, p_idlicencia integer, p_feclicenciaini date, p_feclicenciafin date, p_diagnostico text, p_auditoria boolean, p_observacion text) OWNER TO postgres;

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
-- Name: soportedoc; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.soportedoc (
    id integer NOT NULL,
    idlicencia integer NOT NULL,
    fecreg timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    nombrearchivo character varying(50) NOT NULL,
    hashfile text NOT NULL
);


ALTER TABLE public.soportedoc OWNER TO postgres;

--
-- Name: soportedoc_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.soportedoc ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.soportedoc_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


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
4	223	2	2026-05-18 15:30:11.48811	2026-05-19	2026-06-19	POSTQUIRGICO DE ARTRODESIS DE MUÑECA IZQUIERDA	HVM-2026-01	f	
5	244	2	2026-05-18 15:48:15.559045	2026-05-06	2026-05-21	GASTROENTERITIS AGUDA SEVERA, PIELONEFRITIS AGUDA	HVM-2026-01	f	
6	167	2	2026-05-27 12:56:46.434554	2026-05-19	2026-06-02	TRAUMA MULTIPLE, FRACTURA  DE LA FALANGE PROXIMAL DEL 3ER DEDO MANO IZQ, TRAUMA DE RODILLA 	HVM-2026-01	f	COLABORADORA ENVIADA A REHABILITACION 
7	244	2	2026-05-27 13:03:19.774861	2026-05-21	2026-05-31	GASTRODUODENITIS AGUDA SECUNDARIA A PARASITOSIS INTESTINAL 	HVM-2026-02	f	
8	8	2	2026-05-27 13:06:03.461333	2026-05-26	2026-06-10	EMBARAZO DE 13 SEMANA, INCOMPETENCIA ITSMICO CERVICAL	HVM-2026-01	f	
9	261	2	2026-06-12 19:48:03.627192	2026-06-08	2026-07-08	SINDROME MOTOR CRONICO POSTERIOR A EVENTO CEREBROVASCULAR HEMORRAGICO	HVM-2026-01	t	PACIENTE SE ENVIA AUDITORIA MEDICA EN LA SEDE PARA FINES DE TOMA DE DECISION RELACIONADA A SU ESTADO ACTUAL DE FUNCIONES ADMINISTRACTIVA LABORAL.
10	167	2	2026-06-12 19:59:02.57019	2026-06-03	2026-06-18	FRACTURA DE LA FALANGE PROXIMAL DEL 3ER DEDO MANO IZQUIERDA	HVM-2026-02	f	COLABORADOR LLEVANDO CASO POR LA IDOPPRIL EN FISIOTERAPIA
11	168	2	2026-06-12 20:07:58.198268	2026-06-09	2026-06-30	LESION DEL LABRUM GLENOIDE SLAP 2	HVM-2026-01	f	COLABORADOR EVALUADO Y PREPARANDO PARA FINES DE CIRUGIA DE HOMBRE Y EVALUADO POR IDOPPRIL POR ENF. OCUPACIONAL
12	229	2	2026-06-12 20:11:34.723781	2026-06-02	2026-06-12	HERIDA CORTANTE EN PIE DERECHO DE 10-13CM	HVM-2026-01	f	
13	244	2	2026-06-15 20:29:50.53453	2026-06-11	2026-06-26	TRASTORNO PSIQUIATRICO NO ESPECIFICO P/B DISCAPACIDAD INTELECTAL 	HVM-2026-03	f	COLABORADOR EN SEGUIMIENTO DE PSIQUIATRIA
14	167	2	2026-06-19 15:27:21.273933	2026-06-18	2026-07-09	FRACTURA DE LA FALANGE PROXIMAL DEL 3ER DEDO MANO IZQUIERDA	HVM-2026-03	f	COLABORADOR EN SEGUIMIENTO DE CASO POR EL IDOPPRIL Y FISITRIA. 
15	8	2	2026-06-19 15:35:54.479601	2026-06-18	2026-07-09	EMBARAZO DE 17 SEMANA, INCOMPETENCIA ITSMICO CERVICAL CON SIGNO DE FUNNELING TIPO V POSITIVO	HVM-2026-02	f	COLABORADORA SE LE LICENCIA RECIBE LICENCIA DE 21 DIAS COMO MAXIMO HASTA NUEVA EVALUACION Y DESICION DE GINECOLOGA
16	223	2	2026-06-19 15:43:17.029861	2026-06-19	2026-07-19	POSTQUIRGICO DE ARTRODESIS DE MUÑECA IZQUIERDA	HVM-2026-02	f	COLABORADORA EN TTO CON SIQUIATRIA POR DEPRESION Y CAMBIOS EMOCIONALES.
22	262	2	2026-06-27 22:06:01.238845	2026-06-22	2026-06-27	Extracción de cordales No. 18 y 28	HVM-2026-01	f	Reposo médico por un período de cinco (5) días a partir de la fecha del procedimiento, evitando actividades físicas intensas, esfuerzos, exposición al sol, alimentos duros o calientes, y siguiendo las indicaciones postquirúrgicas y medicamentos prescritos.
23	196	2	2026-06-30 16:01:32.423311	2026-06-29	2026-07-04	QUISTE SIMPLE DE OVARIO; DOLOR ABDOMINAL NO QUIRÚRGICO; ENF. PELVICO INFLAMATORIO; INFECCION DE VIAS URINARIA	HVM-2026-01	f	Reposo y manejo ambulatorio durante 5 días.
21	157	2	2026-06-27 21:51:21.466369	2026-06-24	2026-07-01	ulcera peptica, gastritis y exeresis de polipo duodenal	HVM-2026-01	f	Se recomiendan 7 dias de reposo con fines de completar tratamiento y continuar con las recomendaciones del medico. Documento expedido a solicitud de la interesada el 2026-06-24.
24	85	2	2026-07-06 14:23:35.028333	2026-07-02	2026-07-09	sindrome vertiginoso	HVM-2026-01	f	reposo de 7 dia
25	244	2	2026-07-10 14:52:20.64856	2026-07-09	2026-07-19	sangrado gastrointestinal bajo	HVM-2026-04	f	10 dias de reposo para fines de estudios endoscopico y tratamiento 
26	8	2	2026-07-21 19:46:41.633431	2026-07-17	2026-08-16	EMBARAZO DE 21 SEM. AMENAZA DE PARTO PRETERMINO, CERCLAJE 	HVM-2026-03	f	REPOSO ABSOLUTO
\.


--
-- Data for Name: personal; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.personal (id, cedula, nombre, apellidos, puestotrabajo) FROM stdin;
1	049-0049375-2	Ramon Amable	Gonzalez Peguero	Prueba
2	023-0025365-5	Eustaquio	Ppp	Prueba
3	04900636483	ELIZABETH	ADAMES DE LA CRUZ DE ROMERO	DIRECTORA
4	15500001837	DANNY	ROMERO RONDON	SUB DIRECTOR
5	04900809262	LETICIA	ANTONIA SOTO CAMILO	MEDICO ANESTESIOLOGO
6	22400367995	EURICK	RHADAMES ORTIZ SANTANA	MEDICO ANESTESIOLOGO
7	04900763006	LILIANA	CAROLINA CUEVAS GONZALEZ	MEDICO ANESTESIOLOGO
8	40221514645	PAOLA	NICOLD ROJAS JEREZ	MEDICO ANESTESIOLOGO
9	03104797646	MAIKO	JOSE MARTINEZ GARABITO	MEDICO ANESTESIOLOGO
10	04900597529	ALBA	ROSA RAMIREZ RAMIREZ	JEFE DE SERVICIO DE PEDIATRIA
11	04900826019	YASMIN	ALTAGRACIA RODRIGUEZ JEREZ	MEDICO PEDIATRA
12	04900813942	ELMER	GUAYASAMIN MORA PERDOMO	MEDICO PEDIATRA
13	00107589517	JOSE	AGUSTIN CRUZ BAUTISTA	MEDICO PEDIATRA
14	40222796944	JOSE	VINICIO BONILLA REYNOSO	MEDICO PEDIATRA
15	04900641772	ODALIS	YANET PEREZ SUERO	MEDICO PEDIATRA
16	05601157505	JUAN	FRANCISCO DEL ORBE BOBIER	MEDICO CIRUJANO
17	05800343641	ROMER	FRANCISCO DE JESUS BRITO	CIRUJANO ( MEDICO GENERAL)
18	04700107032	EVELYN	DEL ROSARIO TAPIA LORA	CIRUJANO
19	04900832884	NOEL	ANGEL PEREZ ROSARIO	DIABETOLOGO
20	04900173032	DIGNA	ELIA GERALDINO CONTRERAS	MEDICO FAMILIAR
21	04900702020	NICAULY	SEPULVEDA VILLAR	MEDICO FAMILIAR
22	04900706633	MAYRENI	CORONADO RAMIREZ	ENCARGADA DE EMERGENCIA (MEDICO EMERGENTOLOGO)
23	04701488209	PAULA	ELIZABETH FELIZ GARCIA	MEDICO EMERGENTOLOGO
24	40221640002	ALBERT	JUST MORA JAQUEZ	MEDICO EMERGENTOLOGO
25	05601404592	FELVIC	ANTONIO REYES QUERO	MEDICO EMERGENTOLOGO
26	15500041122	LUISA	MARIA DE LA ROSA GONZALEZ	MEDICO EMERGENTOLOGO
27	04900489982	TANIA	PATRICIA REGALADO BAUTISTA	ENCARGADA DE CALIDAD (MEDICO GENERAL )
28	04900599152	TERESA	ALTAGRACIA LEON LORA	MEDICO CONSULTA
29	04900310451	AWILDA	MIGUELINA ACOSTA VELASQUEZ	MEDICO CONSULTA
30	00102681517	RAMONA	ABREU SANTOS	MEDICO CONSULTA
31	04900399371	RAMON	ALBERTO MENDOZA CRUZ	MEDICO CONSULTA
32	04900595432	JOSE	DEL CARMEN ACOSTA FARIAS	MEDICO CONSULTA
33	04900810518	ANYI	YOELIZA FRIAS DE RODRIGUEZ	MEDICO GENERAL
34	40222213791	JUANA	CESARINA ALBERTO CASTRO	MEDICO GENERAL
35	40221375351	HECTOR	LUIS REYNOSO ROSARIO	MEDICO GENERAL
36	40220739110	MARCOS	LUIS PEGUERO VELASQUEZ	MEDICO GENERAL
37	04900794035	JANNERY	VASQUEZ FRIAS	MEDICO GENERAL
38	15500032485	ADORFINA	REGALADO ROA	MEDICO GENERAL
39	04900744477	ELSA	MARIA BAUTISTA BELEN	MEDICO GENERAL
40	15500032568	GILBERLIZA	COSME BRITO	MEDICO GENERAL
41	04900784481	MINERVA	MERCEDES LORA SALAZAR DE SOTO	MEDICO GENERAL
42	04900869290	JUAN	CARLOS ACOSTA VIZCAINO	MEDICO GENERAL
43	15500016751	DAVID	ANTONIO ALONZO LEONARDO	MEDICO GENERAL
44	08700177234	CYNTHIA	YRENE TORIBIO HERRERA DE CASTILLO	MEDICO GENERAL
45	04900616816	ROSANNA	MERCEDES URBAEZ ACOSTA	MEDICO GENERAL
46	40222053098	LAURA	MERCEDES VELOZ JIMENEZ	MEDICO GENERAL
47	15500061591	SORAIDA	GERMOSEN OTAÑEZ	MEDICO GENERAL
48	40225212774	JHANSEL	ARMANDO PEGUERO GUZMAN	MEDICO GENERAL
49	15500073703	ANYI	RAFAELINA FRIAS JIMENEZ DE REGALADO	MEDICO GENERAL
50	40226624712	KAROLIN	OSCARINA JEREZ GUZMAN	MEDICO GENERAL ( MEDICO DE CONCURSO)
51	40223100401	AMALFI	ESTHELA QUEZADA MIESES	MEDICO GENERAL
52	22500212109	DIAMILKA	MARIA ABREU GUZMAN	ENC. DEL DEPARTAMENTO DE AUDITORIA ( MEDICO SONOGRAFISTA)
53	40221215185	INES	ALTAGRACIA GARCIA PEREZ DE GONZALEZ	MEDICO AUDITOR (MEDICO GENERAL)
54	04900628878	ROSARIO	YNMACULADA REINOSO PAREDES	MEDICO GINECOBSTETRA
55	00116570508	MABELIZA	ABREU VARGAS	MEDICO GINECOBSTETRA
56	04900724255	ELI	MARIA BAUTISTA SANCHEZ	MEDICO GINECOBSTETRA
57	04900842560	DANILKA	GOMEZ RONDON	MEDICO GINECOBSTETRA
58	00107238834	GARIBALDI	SIBILIA DE LOS SANTOS	MEDICO GINECOBSTETRA
59	04900155252	ALEJANDRO	TRINIDAD RODRIGUEZ	MEDICO INTERNISTA
60	04900707722	LISSETT	DEL CARMEN ACOSTA ABREU	MEDICO INTERNISTA
61	04900832397	LIDIA	ABREU PAREDES	MEDICO INTERNISTA
62	40220140491	ANYELA	EDIT TAVAREZ TAVERAS	MEDICO INTERNISTA
63	04900515810	GRACIELA	YNMACULADA REGALADO GRULLON	MEDICO SONOGRAFISTA
64	04900718638	JOHNALTON	ALBERTO GARABITO MONTILLA	MEDICO SONOGRAFISTA
65	04900809890	WINEL	ANTONIO REYES ALMONTE	MEDICO SONOGRAFISTA
66	04900023385	JUANA	SANCHEZ	ENCARGADO DE CUIDADOS DE ENFERMERIA EN HOSPITALIZACION
67	04900373921	MARGARITA	ALMONTE HERRERA	ENCARGADO(A) DE CUIDADOS AMBULATORIO
68	04900379985	LEONIDA	PAULINO FARIAS DE REGALADO	ENFERMERO (A) ATENCION DIRECTA
69	04900653181	SORINILDA	BENITEZ RONDON DE TAVAREZ	ENFERMERO (A) ATENCION DIRECTA
70	04900482342	ZOILA	FLORENTINO GOMEZ DE JIMENEZ	ENFERMERO (A) ATENCION DIRECTA
71	04900663784	YUBERKY	ADAMES MATA DE FARIAS	ENFERMERO (A) ATENCION DIRECTA
72	04900691926	ANGELA	FLORENTINO GOMEZ DE JOSE	ENFERMERO (A) ATENCION DIRECTA
73	04900638570	YLUMINADA	REGALADO CAMBERO	ENFERMERO (A) ATENCION DIRECTA
74	15500007891	YUNI	CAROLINA MORILLO PEGUERO	ENFERMERO (A) ATENCION DIRECTA
75	04900106792	MARINA	VASQUEZ BRITO	ENFERMERO (A) ATENCION DIRECTA
76	04900776784	DANIELA	SANCHEZ DE GERALDINO	ENFERMERO (A) ATENCION DIRECTA
77	04900493448	MARIA	CRISTINA HERRERA DE CONCEPCION	ENFERMERO (A) ATENCION DIRECTA
78	04900628860	NANCY	YACIRI CRUCETA REINOSO DE GIL	ENFERMERO (A) ATENCION DIRECTA
79	04900114200	CARMEN	GUTIERREZ ORTEGA DE AMPARO	ENFERMERO (A) ATENCION DIRECTA
80	04900495211	ANA	HERNANDEZ MARTINEZ	ENFERMERO (A) ATENCION DIRECTA
81	04900754344	YALGELY	ORTEGA DE RODRIGUEZ	ENFERMERO (A) ATENCION DIRECTA
82	15500017825	MILY	HERNANDEZ MENA	ENFERMERO (A) ATENCION DIRECTA
83	04900350812	SANDRA	FIORDALIZA ACOSTA TAVERA	ENFERMERO (A) ATENCION DIRECTA
84	40220183152	FANNY	CELENNY MOYA FLORENTINO	ENFERMERO (A) ATENCION DIRECTA
85	04900379597	MARIA	SALOME MOTA CARRASCO	ENFERMERA DE ATENCION DIRECTA
86	04900561780	ANGELA	SUAREZ RONDON	ENFERMERA DE ATENCION  DIRECTA
87	04900387616	LUCILA	JOSEFINA RODRIGUEZ DUVERGE	AUXILIAR DE ENFERMERIA
88	04900492432	ANA	FIORDALIZA JIMENEZ RINCON	AUXILIAR DE ENFERMERIA
89	04900210545	BRIGIDA	GOMEZ CASTILLO DE ADAMES	AUXILIAR DE ENFERMERIA
90	04900483878	ANNERY	JOSEFINA MENDEZ VASQUEZ	AUXILIAR DE ENFERMERIA
91	04900379795	LIDIA	UREÑA ORTIZ	AUXILIAR DE ENFERMERIA
92	04900501752	ORFELINA	DEL CARMEN JOAQUIN	AUXILIAR DE ENFERMERIA
93	04900170921	FRANCISCA	ISABEL VERAS FABIAN DE MARTE	AUXILIAR DE ENFERMERIA
94	04900802861	ARISLEYDA	CLARIBEL DISLA PICHARDO	AUXILIAR DE ENFERMERIA
95	04900349327	MARIA	MERCEDES A NUÑEZ MEJIA	AUXILIAR DE ENFERMERIA
96	04900153166	JUANA	MOREL PERALTA	AUXILIAR DE ENFERMERIA
97	04900380454	FIDELINA	REYNOSO FARIAS	AUXILIAR DE ENFERMERIA
98	08700167664	JOSE	RAFAEL CABA HERRERA	AUXILIAR DE ENFERMERIA
99	04900564263	ZAIRA	CASTRO COLON	AUXILIAR DE ENFERMERIA
100	04900475437	EUSEBIA	REGALADO BENITEZ	AUXILIAR DE ENFERMERIA
101	04900559040	KENIA	MERCEDES DE JESUS	AUXILIAR DE ENFERMERIA
102	12200040215	MERCEDES	ACOSTA DE JESUS	AUXILIAR DE ENFERMERIA
103	12200007784	MARGARITA	ACOSTA RODRIGUEZ	AUXILIAR DE ENFERMERIA
104	03100573397	MARTINA	MEJIA RAMOS	AUXILIAR DE ENFERMERIA
105	04900680754	TORIBIO	PAULINO FARIAS	AUXILIAR DE ENFERMERIA
106	04900601875	MARTHA	YRIS VASQUEZ MANZUETA	AUXILIAR DE ENFERMERIA
107	22400096487	JENNIFER	DOTEL VIOLA	AUXILIAR DE ENFERMERIA
108	04900716103	YUBERKY	GARCIA HERNANDEZ	AUXILIAR DE ENFERMERIA
109	04900596463	MARIA	ALTAGRACIA FRIAS HILARIO	AUXILIAR DE ENFERMERIA
110	15500058407	ROSA	ESTRELLA VILLAFAÑA CASTRO	AUXILIAR DE ENFERMERIA
111	04900161318	YENNY	ALTAGRACIA HERNANDEZ BRITO	AUXILIAR DE ENFERMERIA
112	04900004542	EVA	CRUCELINA ABREU GARCIA	ENCARGADA DE SERVICIO DE LABORATORIO
113	04900311327	YSA	MERCEDES GONZALEZ REYES	ENCARGADA UNIDAD DE BASILOSCOPIA
114	15500011091	YARITZA	ESTEVEZ MARIA	SECRETARIA DE LABORATORIO (SECRETARIA)
115	04900589302	MARIA	ELENA SOSA RONDON	ENCARGADO(A) DE LA UNIDAD DE HEMATOLOGIA
116	04900674013	DAIANA	LISSETTE SALDAÑA FABIAN	ENCARGADO(A) DE LA UNIDAD DE SEROLOGIA
117	04900374671	IVELISSE	MERCEDES DIAZ ABREU	ENCARGADO (A) DE LA UNIDAD DE TOMA DE MUESTRA
118	40222412625	WENDY	ACOSTA DE DUARTE	ENCARGADA DEL AREA DE QUIMICA CLINICA
119	04900717937	RAQUEL	ROSARIO VASQUEZ DE VILORIA	ENCARGADA UNIDAD DE QUIMICA CLINICA
120	15500042963	YOELISA	SUERO DISLA	ENCARGADO(A) UNIDAD DE PRUEBAS ESPECIALES
121	04900633308	MARCELINA	MIRAMBEAUX	ENCARGADO(A) UNIDAD DE PARASITOOGIA Y UROANALISIS
122	04900864911	LUZ	ATANY HERRERA LIRIANO	BIOANALISTA
123	04900353006	YDALIA	GONZALEZ CORDERO DE ACOSTA	BIOANALISTA
124	04900018252	JUANA	YRIS ACOSTA	BIOANALISTA
125	00107064032	JOSEFINA	JHOVANY RODRIGUEZ HERRERA	BIOANALISTA
126	04900414097	YOANI	DE JESUS RONDON GOMEZ	BIOANALISTA
127	04900575509	ALEJANDRINA	SANTO DE DEL ORBE	BIOANALISTA
128	04900723851	ANA	ADAMES DE REGALADO	BIOANALISTA
129	08700159125	RAMONA	HERNANDEZ DISLA	BIOANALISTA
130	15500040371	MARIA	DEL CARMEN JIMENEZ AZCONA	BIOANALISTA
131	15500023526	JUANA	FRANCISCA SUERO ADAMES	BIOANALISTA
132	04900750979	YUDY	ALTAGRACIA MARIA MARIA	BIOANALISTA
133	04900711823	INOCENCIA	REYNOSO MEDINA DE MORILLO	BIOANALISTA
134	04900580137	JUDIT	ARACENA POLANCO	BIOANALISTA
135	15500009574	YUDITH	MARGARITA OZORIA GIL	BIOANALISTA
136	04900639198	MAGALYS	DE JESUS MEDINA DE ESPINOSA	BIOANALISTA
137	15500030992	JUANA	ROSELY SUERO CONCEPCION	BIOANALISTA
138	08700143277	JOSE	GERTRUDIS RODRIGUEZ SUERO	BIOANALISTA
139	40226247258	ROSA	EMILIA MATIAS BENITEZ	BIOANALISTA
140	04900618598	LUISA	ADALGIZA LARA AMPARO	BIOANALISTA
141	04900471808	BRUNILDA	HERRERA DE RAMOS	BIOANALISTA
142	08700174561	AGRIERCI	JOSEFINA MEJIA CRUZ	BIOANALISTA
143	12200048374	CRISTOBALINA	CABRERA TAVAREZ	BIOANALISTA
144	04900810211	ANA	LUISA MARIA ORTEGA VELOZ	BIOANALISTA
145	15500047418	MARLIN	MARIA CALDERON MARTINEZ	BIOANALISTA
146	04900496235	ENGELS	NICOLAS LIRANZO LIZ	ENCARGADO DE RAYOS X (MEDICO RADIOLOGO)
147	04900864820	YAMELQUI	STERLING HOLGUIN MATIAS	MEDICO RADIOLOGO
148	04900894546	XIOMARA	MIGUELINA MATOS ALMANZAR	LICENCIADA EN IMÁGENES MEDICAS
149	04900516966	RAUL	ANTONIO REGALADO PEREZ	TECNICO RAYOS X
150	40212439208	YORGENIS	CORCINO FRIAS	TECNICO RAYOS X
151	04900865728	YANIVEL	MOREL SANTOS	TECNICO RAYOS X
152	04900637036	ELENA	SANCHEZ ALEJO	TECNICO RAYOS X
153	04900663214	JUANA	GEORGINA AMPARO AMPARO	TECNICO RAYOS X
154	40215331642	JUAN	CARLOS PIMENTEL	TECNICO RAYOS X
155	04900705213	LORENZA	DE JESUS BRITO	TECNICO RAYOS X
156	40214072726	ROSSIRY	GLENCHA VILLAMAN GARCIA	SECRETARIA DE RAYOS X (DIGITADORA)
157	04900022999	BRIGIDA	RODRIGUEZ ABREU	ENCARGADA DE PSICOLOGIA (PSICOLOGA)
158	15500028046	YARISSA	ICEFIELD GONZALEZ DE RODRIGUEZ	PSICOLOGA
159	04900562952	LEOCADIA	MOTA DE CABREJA	PSICOLOGA
160	04900538754	MARIA	YSABEL DE LA ROSA BASORA	PSICOLOGA
161	04900713720	ROSAURI	SALCEDO SANCHEZ	ENCARGADA DE EPIDEMIOLOGIA (MEDICO GENERAL)
162	08700035507	CARMEN	MIGUELINA SALDAÑA CHAVEZ	AUXILIAR DE EPIDEMIOLOGIA (ENCARGADA DE EPIDEMIOLOGIA)
163	04900022197	MAYRA	MARIA PEGUERO FABIAN	ENCARGADA DE FARMACIA
164	40237614389	JESSICA	GARCIA NUÑEZ	AUXILIAR DE FARMACIA
165	00118382027	INGRID	YOELY GARCIA FRIAS	AUXILIAR DE FARMACIA
166	15500034085	IVELISSE	DOMINGA MINIER MERCEDES	AUXILIAR FARMACIA (PROMOTOR DE SALUD)
167	15500059009	VIANNELLY	ALGRACIA RONDON VASQUEZ	ENCAERGADA ALMACEN (TECNICO CONTABILIDAD)
168	04900490485	MARIA	DINORAH OTAÑEZ	AUXILIAR DE FARMACIA
169	40224564167	NEUBELIS	PEREZ HENRIQUEZ	ENC. DE FACTURACION Y SEGUROS
170	15500044035	YINET	ROSARIO GRACIA	AUXILIAR DE FACTURACION
171	04900777162	YANET	REINOSO	AUXILIAR FACTURACION
172	04900588908	JESUCITA	PEÑA SICARDO	AUXILIAR DE FACTURACION
173	15500053564	KATHIA	ESPINAL ROSARIO	AUXILIAR FACTURACION (SECRETARIA)
174	15500059405	ANA	GABRIELA VASQUEZ FERNANDEZ	AUXILIAR DE FACTURACION
175	40242522072	ALEXANDER	JUNIOR POLANCO NICASIO	AUXILIAR DE FACTURACION
176	04900695752	NOEMI	BIENVENIDA CORONA ROMERO	AUXILIAR DE FACTURACION
177	15500072408	MARIA	MERCEDES REYNOSO	AUXILIAR DE FACTURACION
178	40211659277	NATHANAEL	UREÑA ORTEGA	AUXILIAR DE FACTURACION Y SEGURO
179	40213583491	KAREN	ROSARIO FERNANDEZ	AUXILIAR DE FACTURACION Y SEGURO
180	15500025059	YAJAIRA	HERNANDEZ	AUXILIAR DE FACTURACION
181	40225643044	ANGIE	RANDIELY SANTANA RAMIREZ	AUXILIAR DE FACTURACION (AUXILIAR ADMINISTRATIVO)
182	05700155996	YULISANDRA	VALDEZ REGALADO	ENCARGADA ESTADISTICA (TECNICO ESTADISTICO)
183	04900726771	ANGELITA	ADAMES ADAMES	TECNICO ESTADISTICO
184	40212808295	ELIANNY	ZELINE PEREZ LIZ	ENCARGADA DE ARCHIVO (TECNICO ARCHIVISTA)
185	40224866315	REGINO	ACOSTA REGALADO	ENCARGADO ATENCION AL USUARIO(OFICIAL DE ATENCION AL USUARIO)
186	40222601474	MANUEL	RAFAEL TORRES PAVON	AUXILIAR DE ATENCION AL USUARIO
187	15500060049	CLAUDIA	DEL ROSARIO	AUXILIAR DE ATENCION AL USUARIO
188	15500008873	JENNIFER	HIDALGO FIGUEROA	ENCARGADA DE ODONTOLOGIA (ODONTOLOGA)
189	15500061161	ANGELICA	VICTORIA DIAZ DE VASQUEZ	ODONTOLOGA
190	40224343778	ROELVI	SANCHEZ BAUTISTA	ODONTOLOGA
191	00113870976	JOSE	AMERICO GIL SIERRA	ODONTOLOGO
192	40221125475	WANDA	DORIMIL NUÑEZ SANCHEZ	ODONTOLOGA
193	40235471014	ALINA	MERCEDES GONZALEZ CAMACHO	AUXILIAR DENTAL
194	15500055288	LEIDY	MARIA DIAZ BRITO	AUXILIAR DENTAL
195	04900498397	ROSMERY	YANET ALMANZAR REYES	ENCARGADA DE RECURSOS HUMANOS (ANALISTA DE RRHH)
196	40228676868	NICOL	REGALADO NUÑEZ	TECNICO DE RECURSOS HUMANOS ( RECEPCIONISTA)
197	15500074743	SELENNY	DEL CARMEN PEREZ LIZ	ENCARGADO (A) UNIDAD DE GESTION FINANCIERA Y ADMINISTRATIVA
198	40224702155	LUIGGI	FRANCISCO RONDON SANTOS	PORTERO
199	04900758907	CONFESOR	PORTORREAL DE LA CRUZ	PORTERO (AYUDANTE DE MANTENIMIENTO)
200	04900595499	RAMON	ROSA FRIAS	PORTEROS (CAMILLERO)
201	40215242336	ABRAHAN	FRIAS PAULINO	VIGILANTE (CONSERJE)
202	15500070816	DILSON	ORTEGA MEJIA	VIGILANTE
203	40231804036	LUIS	MIGUEL RODRIGUEZ MARTINEZ	VIGILANTE
204	15500044852	CRISTIAN	SANTOS RODRIGUEZ	VIGILANTE
205	40249537081	FELIPE	MOTA ACOSTA	VIGILANTE
206	04900147424	LEONCIO	GARCIA CAMBERO	VIGILANTE
207	15500050255	JUNIOR	DE JESUS HENRIQUEZ MERCEDES	VIGILANTE
208	40223628823	WILMY	ANTONIO SANTOS RODRIGUEZ	VIGILANTE
209	15500063423	JOSELIN	TAVAREZ DIAZ	VIGILANTE (CHOFER)
210	15500066053	GEROMINO	MONEGRO	CAMILLERO
211	04900412802	JUAN	FRANCISCO MARTE GARCIA	CHOFER
212	04900592728	CARLOS	LENIN PEÑA REGALADO	SUPERVISOR DE MANTENIMIENTO
213	15500076417	ELIANNY	CORTORREAL MARIA	GESTOR REDES SOCIALES
214	04900842800	EDDY	ROBERTO SANTOS	ENCARGADO HOTLERIA (SUPERVISOR)
215	08700019709	RAMON	CORNELIO BENZAN SALDAÑA	ELECTRICISTA
216	15500019573	JOSE	MANUEL REYES RESYES	ELECTRICISTA (AYUDANTE DE MANTENIMIENTO)
217	04900609019	BOLIVAR	PAULA GONZALEZ	ELECTRICISTA (AYUDANTE DE MANTENIMIENTO)
218	04900615685	SAUL	QUEZADA ORTEGA	AYUDANTE DE MANTENIMIENTO (TECNICO DE CONTABILIDAD)
219	04900503360	ALTAGRACIA	HENRIQUEZ DIAZ	LAVANDERA
220	04900158256	SANTA	MARIA PERALTA	LAVANDERA
221	04900416761	VENTURA	RAMOS	LAVANDERA (CONSERJE)
222	15500015498	YAMAIRA	ESTEFANI VALDEZ SANTOS	CONSERJE
223	15500019300	BITHANIA	FRIAS RONDON	CONSERJE
224	04900648496	ZUNILDA	REGALADO HERRERA	CONSERJE
225	04900513583	JOSEFINA	DE LOS SANTOS GUTIERREZ SUAZO	CONSERJE
226	04900642176	JULISSA	ALMANZAR CRUZ	CONSERJE
227	04900529886	SATURNINA	FIGUEROA CASTILLO	CONSERJE
228	15500014343	MATILDA	ARGENTINA MENDEZ CORONADO	CONSERJE
229	15500064777	MARIBEL	ACOSTA CRUZ	CONSERJE
230	04900775752	BENITA	MARIA JEREZ AGRAMONTE	CONSERJE
231	15500058449	NAFTALY	VEGA PEGUERO	CONSERJE
232	04900599202	FELICIA	RODRIGUEZ DIAZ	CONSERJE
233	04900588528	GABRIELA	SANCHEZ DIAZ	CONSERJE
234	04900698525	HERIDANIA	ALTAGRACIA RODRIGUEZ SANTIAGO	CONSERJE
235	15500053762	CAROLINA	FLORENTINO PEREZ	CONSERJE
236	04900306216	ELSA	MARIA HERNANDEZ GARCIA	CONSERJE
237	15500070022	FRANYELIS	ROSELIN VERAS PEÑA	COCINERA
238	05200045507	TERESA	REGALADO VIDO	COCINERA
239	00300811262	EVELYN	SORIBEL GUERRERO VILLAR	COCINERA
240	04900742018	MAGALY	ALTAGRACIA REYES AMPARO	COCINERA
241	04900161664	MIGUELINA	JOSE	COCINERA
242	04900672876	JOSEFINA	BASORA MOREL DE NOLAZCO	COCINERA
243	40236210700	YOKAREN	CASTILLO JAVIER	COCINERA
244	04900159262	RAFAEL	GOMEZ TAPIA	JARDINERO
245	04900164411	DEYANIRA	ALTAGRAIA SANTOS MANZUETA	DEPENSISTA (LAVANDERA)
246	04900151301	ESTEBAN	DIPLAN CASTILLO	SOPORTE TECNICO INFORMATICO (DIGITADOR)
247	15500007511	SENIA	FARIAS RONDON	TECNICO ADMINISTRATIVO
248	04900379332	GERTRUDIS	MEJIA ADAMES	ASISTENTE SOCIAL (SUPERVISORA DE PROMOTORES)
249	04900856859	SERPI	ANTONIO POLANCO SOTO	MENSAJERO INTERNO
250	15500070170	MISSAEL	DIAZ GARCIA	MENSAJERO INTERNO  (PROMOTOR DE SALUD)
251	40227031792	FREDERICK	HERNANDEZ ALBERTO	MENSAJERO INTERNO
252	40221967082	WENDY	NAFTALI NIVAR MEJIA DE MARIA	ENCARGADA DE COMPRAS Y CONTRATACIONES (ANALISTA DE COMPRAS Y CONTRATACIONES)
253	15500078116	ARIELFI	BENITEZ REYNOSO	TECNICO COMPRAS Y CONTRATACIONES (RECEPCIONISTA)
254	15500046634	CLARA	ESMERLYN ALMONTE HERNANDEZ	CONTADOR
255	15500038011	ESTEFANIA	MARTINEZ HIDALGO	ENCARGADA LEGAL (ANALISTA LEGAL )
256	04900544059	RUBEN	DARIO NUÑEZ GARCIA	OFICIAL DE ACCESO A LA INFORMACION
257	40225773676	ROSELY	VIANEL DIAZ REYES	ENCARGADA PLANIFICACION Y DESAROLLO
258	04900777352	FIORDALIZA	DEL CARMEN GONZALEZ GARCIA DE ALMANZAR	TECNICO CONTROL DE BIENES
259	40220315366	ISA	XIOMARA ROMANO PEÑA	ANALISTA FINANCIERA
260	40224137634	ALBA	MARINA REYES CRUZ	ANALISTA CALIDAD EN LA GESTION
261	04900659634	ANA LUZ 	MARTINEZ SANTOS	CONSERJE
262	40229017864	KEYLA XOCHIL	LARA SOSA	ODONTOLOGO
\.


--
-- Data for Name: soportedoc; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.soportedoc (id, idlicencia, fecreg, nombrearchivo, hashfile) FROM stdin;
3	21	2026-06-27 21:51:21.466369	SF_27062026_215121_393.png	C0AC9E019F8897A7EA20CF63D824110EB95539DA99189D66A5D1D4C1580BC5A6
4	22	2026-06-27 22:06:01.238845	SF_27062026_220601_223.png	E4CDC67E68A46032C0B991422CC86F26B37722195F598C49B7294F33915574AF
5	23	2026-06-30 16:01:32.423311	SF_30062026_160132_384.png	7710A0426EC8AEC88F9058A7BEFD4AD7BD473E267FA23EA22BD400D4D06EF1B3
6	24	2026-07-06 14:23:35.028333	SF_06072026_142334_989.png	85EA9C1205404F8777688D553EE31ABC7D45880452DB3F1F19C91FB8A9530520
\.


--
-- Data for Name: tbseccion; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.tbseccion (id, idusuario, tokenid, fecinicio, fecexpiracion, isactivo) FROM stdin;
75	2	8b92b8ce-488c-4db5-a325-74b2629d9bca	2026-07-21 22:13:09.761011	2026-07-21 23:13:41.347329	t
\.


--
-- Data for Name: tbuser; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.tbuser (id, nick, pass, rolusuario, activo) FROM stdin;
2	DrPeguero	qaz	1	t
1	Administrador	qaz789	1	f
\.


--
-- Name: licencias_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.licencias_id_seq', 26, true);


--
-- Name: personal_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.personal_id_seq', 262, true);


--
-- Name: soportedoc_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.soportedoc_id_seq', 6, true);


--
-- Name: tbseccion_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.tbseccion_id_seq', 75, true);


--
-- Name: tbuser_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.tbuser_id_seq', 3, true);


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
-- Name: soportedoc soportedoc_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.soportedoc
    ADD CONSTRAINT soportedoc_pkey PRIMARY KEY (id);


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
-- Name: ix_licencias_feclicenciafin; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_licencias_feclicenciafin ON public.licencias USING btree (feclicenciafin);


--
-- Name: ix_licencias_feclicenciaini; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_licencias_feclicenciaini ON public.licencias USING btree (feclicenciaini);


--
-- Name: ix_licencias_fecreg; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_licencias_fecreg ON public.licencias USING btree (fecreg);


--
-- Name: ix_licencias_idpersona; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_licencias_idpersona ON public.licencias USING btree (idpersona);


--
-- Name: ix_licencias_idusuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_licencias_idusuario ON public.licencias USING btree (idusuario);


--
-- Name: ix_personal_cedula_trgm; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_personal_cedula_trgm ON public.personal USING gin (cedula public.gin_trgm_ops);


--
-- Name: ix_personal_nombre_apellidos; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_personal_nombre_apellidos ON public.personal USING btree (nombre, apellidos);


--
-- Name: ix_personal_nombre_trgm; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_personal_nombre_trgm ON public.personal USING gin (((((nombre)::text || ' '::text) || (apellidos)::text)) public.gin_trgm_ops);


--
-- Name: ix_soportedoc_fecreg; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_soportedoc_fecreg ON public.soportedoc USING btree (fecreg);


--
-- Name: ix_soportedoc_idlicencia; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_soportedoc_idlicencia ON public.soportedoc USING btree (idlicencia);


--
-- Name: ix_tbseccion_fecexpiracion; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_tbseccion_fecexpiracion ON public.tbseccion USING btree (fecexpiracion);


--
-- Name: ix_tbseccion_idusuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_tbseccion_idusuario ON public.tbseccion USING btree (idusuario);


--
-- Name: ux_tbseccion_tokenid; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX ux_tbseccion_tokenid ON public.tbseccion USING btree (tokenid);


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
-- Name: soportedoc fk_soportedoc_licencias; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.soportedoc
    ADD CONSTRAINT fk_soportedoc_licencias FOREIGN KEY (idlicencia) REFERENCES public.licencias(id) ON UPDATE CASCADE ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict 7VviKafUg364ZfwXQ4B2LidmVT40QV1QIyE4TfLcjG9OpyDCWHZo7rFEdTGh5we

